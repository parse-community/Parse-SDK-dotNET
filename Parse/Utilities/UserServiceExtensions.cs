using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Internal;
using Parse.Abstractions.Platform.Authentication;
using Parse.Infrastructure.Utilities;

namespace Parse
{
    public static class UserServiceExtensions
    {
        internal static string GetCurrentSessionToken(this IServiceHub serviceHub)
        {
            Task<string> sessionTokenTask = GetCurrentSessionTokenAsync(serviceHub);
            sessionTokenTask.Wait();
            return sessionTokenTask.Result;
        }

        internal static Task<string> GetCurrentSessionTokenAsync(this IServiceHub serviceHub, CancellationToken cancellationToken = default)
        {
            return serviceHub.CurrentUserController.GetCurrentSessionTokenAsync(serviceHub, cancellationToken);
        }

        // TODO: Consider renaming SignUpAsync and LogInAsync to SignUpWithAsync and LogInWithAsync, respectively.
        // TODO: Consider returning the created user from the SignUpAsync overload that accepts a username and password.

        /// <summary>
        /// Creates a new <see cref="ParseUser"/>, saves it with the target Parse Server instance, and then authenticates it on the target client.
        /// </summary>
        /// <param name="serviceHub">The <see cref="IServiceHub"/> instance to target when creating the user and authenticating.</param>
        /// <param name="username">The value that should be used for <see cref="ParseUser.Username"/>.</param>
        /// <param name="password">The value that should be used for <see cref="ParseUser.Password"/>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static Task SignUpAsync(this IServiceHub serviceHub, string username, string password, CancellationToken cancellationToken = default)
        {
            return new ParseUser { Services = serviceHub, Username = username, Password = password }.SignUpAsync(cancellationToken);
        }

        /// <summary>
        /// Saves the provided <see cref="ParseUser"/> instance with the target Parse Server instance and then authenticates it on the target client. This method should only be used once <see cref="ParseClient.Publicize"/> has been called and <see cref="ParseClient.Instance"/> is the wanted bind target, or if <see cref="ParseObject.Services"/> has already been set or <see cref="ParseObject.Bind(IServiceHub)"/> has already been called on the <paramref name="user"/>.
        /// </summary>
        /// <param name="serviceHub">The <see cref="IServiceHub"/> instance to target when creating the user and authenticating.</param>
        /// <param name="user">The <see cref="ParseUser"/> instance to save on the target Parse Server instance and authenticate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static Task SignUpAsync(this IServiceHub serviceHub, ParseUser user, CancellationToken cancellationToken = default)
        {
            user.Bind(serviceHub);
            return user.SignUpAsync(cancellationToken);
        }

        /// <summary>
        /// Logs in a user with a username and password. On success, this saves the session to disk or to memory so you can retrieve the currently logged-in user using <see cref="GetCurrentUser(IServiceHub)"/>.
        /// </summary>
        /// <param name="serviceHub">The <see cref="IServiceHub"/> instance to target when logging in.</param>
        /// <param name="username">The username to log in with.</param>
        /// <param name="password">The password to log in with.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The newly logged-in user.</returns>
        public static async Task<ParseUser> LogInAsync(this IServiceHub serviceHub, string username, string password, CancellationToken cancellationToken = default)
        {
            // Log in the user and get the user state
            var userState = await serviceHub.UserController
                .LogInAsync(username, password, serviceHub, cancellationToken)
                .ConfigureAwait(false);

            // Generate the ParseUser object from the returned state
            var user = serviceHub.GenerateObjectFromState<ParseUser>(userState, "_User");

            // Save the user locally
            await SaveCurrentUserAsync(serviceHub, user).ConfigureAwait(false);

            // Set the authenticated user as the current instance
            InstanceUser = user;

            return user;
        }

        public static ParseUser InstanceUser { get; set; }


        /// <summary>
        /// Logs in a user with a username and password. On success, this saves the session to disk so you
        /// can retrieve the currently logged-in user using <see cref="GetCurrentUser()"/>.
        /// </summary>
        /// <param name="sessionToken">The session token to authorize with</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The user if authorization was successful</returns>
        public static async Task<ParseUser> BecomeAsync(this IServiceHub serviceHub, string sessionToken, CancellationToken cancellationToken = default)
        {
            // Fetch the user state using the session token
            var userState = await serviceHub.UserController.GetUserAsync(sessionToken, serviceHub, cancellationToken).ConfigureAwait(false);

            // Generate the ParseUser object from the returned state
            var user = serviceHub.GenerateObjectFromState<ParseUser>(userState, "_User");

            // Save the user locally
            await SaveCurrentUserAsync(serviceHub, user).ConfigureAwait(false);

            // Set the authenticated user as the current instance only after successful save
            InstanceUser = user;

            return user;
        }



        /// <summary>
        /// Logs out the currently logged in user session. This will remove the session from disk, log out of
        /// linked services, and future calls to <see cref="GetCurrentUser()"/> will return <c>null</c>.
        /// </summary>
        /// <remarks>
        /// Typically, you should use <see cref="LogOutAsync()"/>, unless you are managing your own threading.
        /// </remarks>
        public static void LogOut(this IServiceHub serviceHub)
        {
            LogOutAsync(serviceHub).Wait(); // TODO (hallucinogen): this will without a doubt fail in Unity. But what else can we do?
        }

        /// <summary>
        /// Logs out the currently logged in user session. This will remove the session from disk, log out of
        /// linked services, and future calls to <see cref="GetCurrentUser()"/> will return <c>null</c>.
        /// </summary>
        /// <remarks>
        /// This is preferable to using <see cref="LogOut()"/>, unless your code is already running from a
        /// background thread.
        /// </remarks>
        public static Task LogOutAsync(this IServiceHub serviceHub)
        {
            return LogOutAsync(serviceHub, CancellationToken.None);
        }

        /// <summary>
        /// Logs out the currently logged in user session. This will remove the session from disk, log out of
        /// linked services, and future calls to <see cref="GetCurrentUser(IServiceHub)"/> will return <c>null</c>.
        ///
        /// This is preferable to using <see cref="LogOut()"/>, unless your code is already running from a
        /// background thread.
        /// </summary>
        public static async Task LogOutAsync(this IServiceHub serviceHub, CancellationToken cancellationToken)
        {
            // Fetch the current user
            var user = await GetCurrentUserAsync(serviceHub).ConfigureAwait(false);

            // Log out with providers
            LogOutWithProviders();

            // If a user is logged in, log them out and return the result, otherwise, complete immediately
            if (user != null)
            {
                await user.TaskQueue.Enqueue(toAwait => user.LogOutAsync(cancellationToken), cancellationToken).ConfigureAwait(false);
            }
        }


        static void LogOutWithProviders()
        {
            foreach (IParseAuthenticationProvider provider in ParseUser.Authenticators.Values)
            {
                provider.Deauthenticate();
            }
        }

        /// <summary>
        /// Gets the currently logged in ParseUser with a valid session, either from memory or disk
        /// if necessary.
        /// </summary>
        public static ParseUser GetCurrentUser(this IServiceHub serviceHub)
        {
            Task<ParseUser> userTask = GetCurrentUserAsync(serviceHub);

            // TODO (hallucinogen): this will without a doubt fail in Unity. How should we fix it?

            userTask.Wait();
            return userTask.Result;
        }

        /// <summary>
        /// Gets the currently logged in ParseUser with a valid session, either from memory or disk
        /// if necessary, asynchronously.
        /// </summary>
        internal static Task<ParseUser> GetCurrentUserAsync(this IServiceHub serviceHub, CancellationToken cancellationToken = default)
        {
            return serviceHub.CurrentUserController.GetAsync(serviceHub, cancellationToken);
        }

        internal static Task SaveCurrentUserAsync(this IServiceHub serviceHub, ParseUser user, CancellationToken cancellationToken = default)
        {
            return serviceHub.CurrentUserController.SetAsync(user, cancellationToken);
        }

        internal static void ClearInMemoryUser(this IServiceHub serviceHub)
        {
            serviceHub.CurrentUserController.ClearFromMemory();
        }

        /// <summary>
        /// Constructs a <see cref="ParseQuery{ParseUser}"/> for <see cref="ParseUser"/>s.
        /// </summary>
        public static ParseQuery<ParseUser> GetUserQuery(this IServiceHub serviceHub)
        {
            return serviceHub.GetQuery<ParseUser>();
        }

        #region Legacy / Revocable Session Tokens

        /// <summary>
        /// Tells server to use revocable session on LogIn and SignUp, even when App's Settings
        /// has "Require Revocable Session" turned off. Issues network request in background to
        /// migrate the sessionToken on disk to revocable session.
        /// </summary>
        /// <returns>The Task that upgrades the session.</returns>
        //public static Task EnableRevocableSessionAsync(this IServiceHub serviceHub, CancellationToken cancellationToken = default)
        //{
        //    lock (serviceHub.UserController.RevocableSessionEnabledMutex)
        //    {
        //        serviceHub.UserController.RevocableSessionEnabled = true;
        //    }

        //    return GetCurrentUserAsync(serviceHub, cancellationToken).OnSuccess(task => task.Result.UpgradeToRevocableSessionAsync(cancellationToken));
        //}

        //internal static void DisableRevocableSession(this IServiceHub serviceHub)
        //{
        //    lock (serviceHub.UserController.RevocableSessionEnabledMutex)
        //    {
        //        serviceHub.UserController.RevocableSessionEnabled = false;
        //    }
        //}

        //internal static bool GetIsRevocableSessionEnabled(this IServiceHub serviceHub)
        //{
        //    lock (serviceHub.UserController.RevocableSessionEnabledMutex)
        //    {
        //        return serviceHub.UserController.RevocableSessionEnabled;
        //    }
        //}

        #endregion

        /// <summary>
        /// Requests a password reset email to be sent to the specified email address associated with the
        /// user account. This email allows the user to securely reset their password on the Parse site.
        /// </summary>
        /// <param name="email">The email address associated with the user that forgot their password.</param>
        public static Task RequestPasswordResetAsync(this IServiceHub serviceHub, string email)
        {
            return RequestPasswordResetAsync(serviceHub, email, CancellationToken.None);
        }

        /// <summary>
        /// Requests a password reset email to be sent to the specified email address associated with the
        /// user account. This email allows the user to securely reset their password on the Parse site.
        /// </summary>
        /// <param name="email">The email address associated with the user that forgot their password.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static Task RequestPasswordResetAsync(this IServiceHub serviceHub, string email, CancellationToken cancellationToken)
        {
            return serviceHub.UserController.RequestPasswordResetAsync(email, cancellationToken);
        }

        public static async Task<ParseUser> LogInWithAsync(this IServiceHub serviceHub, string authType, IDictionary<string, object> data, CancellationToken cancellationToken)
        {
            // Log in the user with the provided authType and data
            var userState = await serviceHub.UserController
                .LogInAsync(authType, data, serviceHub, cancellationToken)
                .ConfigureAwait(false);

            // Generate the ParseUser object from the user state
            var user = serviceHub.GenerateObjectFromState<ParseUser>(userState, "_User");

            // Synchronize the user data in a thread-safe way
            lock (user.Mutex)
            {
                user.AuthData ??= new Dictionary<string, IDictionary<string, object>>();

                user.AuthData[authType] = data;

                // Synchronize authentication data for all providers
                user.SynchronizeAllAuthData();
            }

            // Save the current user locally
            await SaveCurrentUserAsync(serviceHub, user).ConfigureAwait(false);

            return user;
        }

        public static async Task<ParseUser> LogInWithAsync(this IServiceHub serviceHub, string authType, CancellationToken cancellationToken)
        {
            // Get the authentication provider based on the provided authType
            IParseAuthenticationProvider provider = ParseUser.GetProvider(authType);

            // Authenticate using the provider
            var authData = await provider.AuthenticateAsync(cancellationToken).ConfigureAwait(false);

            // Log in using the authenticated data
            return await LogInWithAsync(serviceHub, authType, authData, cancellationToken).ConfigureAwait(false);
        }


        internal static void RegisterProvider(this IServiceHub serviceHub, IParseAuthenticationProvider provider)
        {
            ParseUser.Authenticators[provider.AuthType] = provider;
            ParseUser curUser = GetCurrentUser(serviceHub);

            if (curUser != null)
            {
#pragma warning disable CS1030 // #warning directive
#warning Check if SynchronizeAllAuthData should accept an IServiceHub for consistency on which actions take place on which IServiceHub implementation instance.

                curUser.SynchronizeAuthData(provider);
#pragma warning restore CS1030 // #warning directive
            }
        }
    }
}
