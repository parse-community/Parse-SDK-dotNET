// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using Parse.Core.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Parse.Common.Internal;

namespace Parse
{
    /// <summary>
    /// Represents a user for a Parse application.
    /// </summary>
    [ParseClassName("_User")]
    public class ParseUser : ParseObject
    {
        private static readonly IDictionary<string, IParseAuthenticationProvider> authProviders =
            new Dictionary<string, IParseAuthenticationProvider>();

        private static readonly HashSet<string> readOnlyKeys = new HashSet<string> {
      "sessionToken", "isNew"
    };

        internal static IParseUserController UserController
        {
            get
            {
                return ParseCorePlugins.Instance.UserController;
            }
        }

        internal static IParseCurrentUserController CurrentUserController
        {
            get
            {
                return ParseCorePlugins.Instance.CurrentUserController;
            }
        }

        /// <summary>
        /// Whether the ParseUser has been authenticated on this device. Only an authenticated
        /// ParseUser can be saved and deleted.
        /// </summary>
        public bool IsAuthenticated
        {
            get
            {
                lock (mutex)
                {
                    return SessionToken != null &&
                      CurrentUser != null &&
                      CurrentUser.ObjectId == ObjectId;
                }
            }
        }

        /// <summary>
        /// Removes a key from the object's data if it exists.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <exception cref="System.ArgumentException">Cannot remove the username key.</exception>
        public override void Remove(string key)
        {
            if (key == "username")
            {
                throw new ArgumentException("Cannot remove the username key.");
            }
            base.Remove(key);
        }

        protected override bool IsKeyMutable(string key)
        {
            return !readOnlyKeys.Contains(key);
        }

        internal override void HandleSave(IObjectState serverState)
        {
            base.HandleSave(serverState);

            SynchronizeAllAuthData();
            CleanupAuthData();

            MutateState(mutableClone =>
            {
                mutableClone.ServerData.Remove("password");
            });
        }

        public string SessionToken
        {
            get
            {
                if (State.ContainsKey("sessionToken"))
                {
                    return State["sessionToken"] as string;
                }
                return null;
            }
        }

        internal static string CurrentSessionToken
        {
            get
            {
                Task<string> sessionTokenTask = GetCurrentSessionTokenAsync();
                sessionTokenTask.Wait();
                return sessionTokenTask.Result;
            }
        }

        internal static Task<string> GetCurrentSessionTokenAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return CurrentUserController.GetCurrentSessionTokenAsync(cancellationToken);
        }

        internal Task SetSessionTokenAsync(string newSessionToken)
        {
            return SetSessionTokenAsync(newSessionToken, CancellationToken.None);
        }

        internal Task SetSessionTokenAsync(string newSessionToken, CancellationToken cancellationToken)
        {
            MutateState(mutableClone =>
            {
                mutableClone.ServerData["sessionToken"] = newSessionToken;
            });

            return SaveCurrentUserAsync(this);
        }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        [ParseFieldName("username")]
        public string Username
        {
            get { return GetProperty<string>(null, "Username"); }
            set { SetProperty(value, "Username"); }
        }

        /// <summary>
        /// Sets the password.
        /// </summary>
        [ParseFieldName("password")]
        public string Password
        {
            private get { return GetProperty<string>(null, "Password"); }
            set { SetProperty(value, "Password"); }
        }

        /// <summary>
        /// Sets the email address.
        /// </summary>
        [ParseFieldName("email")]
        public string Email
        {
            get { return GetProperty<string>(null, "Email"); }
            set { SetProperty(value, "Email"); }
        }

        internal Task SignUpAsync(Task toAwait, CancellationToken cancellationToken)
        {
            if (AuthData == null)
            {
                // TODO (hallucinogen): make an Extension of Task to create Task with exception/canceled.
                if (string.IsNullOrEmpty(Username))
                {
                    TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
                    tcs.TrySetException(new InvalidOperationException("Cannot sign up user with an empty name."));
                    return tcs.Task;
                }
                if (string.IsNullOrEmpty(Password))
                {
                    TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
                    tcs.TrySetException(new InvalidOperationException("Cannot sign up user with an empty password."));
                    return tcs.Task;
                }
            }
            if (!string.IsNullOrEmpty(ObjectId))
            {
                TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
                tcs.TrySetException(new InvalidOperationException("Cannot sign up a user that already exists."));
                return tcs.Task;
            }

            IDictionary<string, IParseFieldOperation> currentOperations = StartSave();

            return toAwait.OnSuccess(_ =>
            {
                return UserController.SignUpAsync(State, currentOperations, cancellationToken);
            }).Unwrap().ContinueWith(t =>
            {
                if (t.IsFaulted || t.IsCanceled)
                {
                    HandleFailedSave(currentOperations);
                }
                else
                {
                    var serverState = t.Result;
                    HandleSave(serverState);
                }
                return t;
            }).Unwrap().OnSuccess(_ => SaveCurrentUserAsync(this)).Unwrap();
        }

        /// <summary>
        /// Signs up a new user. This will create a new ParseUser on the server and will also persist the
        /// session on disk so that you can access the user using <see cref="CurrentUser"/>. A username and
        /// password must be set before calling SignUpAsync.
        /// </summary>
        public Task SignUpAsync()
        {
            return SignUpAsync(CancellationToken.None);
        }

        /// <summary>
        /// Signs up a new user. This will create a new ParseUser on the server and will also persist the
        /// session on disk so that you can access the user using <see cref="CurrentUser"/>. A username and
        /// password must be set before calling SignUpAsync.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        public Task SignUpAsync(CancellationToken cancellationToken)
        {
            return taskQueue.Enqueue(toAwait => SignUpAsync(toAwait, cancellationToken),
                cancellationToken);
        }

        /// <summary>
        /// Logs in a user with a username and password. On success, this saves the session to disk so you
        /// can retrieve the currently logged in user using <see cref="CurrentUser"/>.
        /// </summary>
        /// <param name="username">The username to log in with.</param>
        /// <param name="password">The password to log in with.</param>
        /// <returns>The newly logged-in user.</returns>
        public static Task<ParseUser> LogInAsync(string username, string password)
        {
            return LogInAsync(username, password, CancellationToken.None);
        }

        /// <summary>
        /// Logs in a user with a username and password. On success, this saves the session to disk so you
        /// can retrieve the currently logged in user using <see cref="CurrentUser"/>.
        /// </summary>
        /// <param name="username">The username to log in with.</param>
        /// <param name="password">The password to log in with.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The newly logged-in user.</returns>
        public static Task<ParseUser> LogInAsync(string username,
            string password,
            CancellationToken cancellationToken)
        {
            return UserController.LogInAsync(username, password, cancellationToken).OnSuccess(t =>
            {
                ParseUser user = ParseObject.FromState<ParseUser>(t.Result, "_User");
                return SaveCurrentUserAsync(user).OnSuccess(_ => user);
            }).Unwrap();
        }

        /// <summary>
        /// Logs in a user with a username and password. On success, this saves the session to disk so you
        /// can retrieve the currently logged in user using <see cref="CurrentUser"/>.
        /// </summary>
        /// <param name="sessionToken">The session token to authorize with</param>
        /// <returns>The user if authorization was successful</returns>
        public static Task<ParseUser> BecomeAsync(string sessionToken)
        {
            return BecomeAsync(sessionToken, CancellationToken.None);
        }

        /// <summary>
        /// Logs in a user with a username and password. On success, this saves the session to disk so you
        /// can retrieve the currently logged in user using <see cref="CurrentUser"/>.
        /// </summary>
        /// <param name="sessionToken">The session token to authorize with</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The user if authorization was successful</returns>
        public static Task<ParseUser> BecomeAsync(string sessionToken, CancellationToken cancellationToken)
        {
            return UserController.GetUserAsync(sessionToken, cancellationToken).OnSuccess(t =>
            {
                ParseUser user = ParseObject.FromState<ParseUser>(t.Result, "_User");
                return SaveCurrentUserAsync(user).OnSuccess(_ => user);
            }).Unwrap();
        }

        protected override Task SaveAsync(Task toAwait, CancellationToken cancellationToken)
        {
            lock (mutex)
            {
                if (ObjectId == null)
                {
                    throw new InvalidOperationException("You must call SignUpAsync before calling SaveAsync.");
                }
                return base.SaveAsync(toAwait, cancellationToken).OnSuccess(_ =>
                {
                    if (!CurrentUserController.IsCurrent(this))
                    {
                        return Task.FromResult(0);
                    }
                    return SaveCurrentUserAsync(this);
                }).Unwrap();
            }
        }

        internal override Task<ParseObject> FetchAsyncInternal(Task toAwait, CancellationToken cancellationToken)
        {
            return base.FetchAsyncInternal(toAwait, cancellationToken).OnSuccess(t =>
            {
                if (!CurrentUserController.IsCurrent(this))
                {
                    return Task<ParseObject>.FromResult(t.Result);
                }
                // If this is already the current user, refresh its state on disk.
                return SaveCurrentUserAsync(this).OnSuccess(_ => t.Result);
            }).Unwrap();
        }

        /// <summary>
        /// Logs out the currently logged in user session. This will remove the session from disk, log out of
        /// linked services, and future calls to <see cref="CurrentUser"/> will return <c>null</c>.
        /// </summary>
        /// <remarks>
        /// Typically, you should use <see cref="LogOutAsync()"/>, unless you are managing your own threading.
        /// </remarks>
        public static void LogOut()
        {
            // TODO (hallucinogen): this will without a doubt fail in Unity. But what else can we do?
            LogOutAsync().Wait();
        }

        /// <summary>
        /// Logs out the currently logged in user session. This will remove the session from disk, log out of
        /// linked services, and future calls to <see cref="CurrentUser"/> will return <c>null</c>.
        /// </summary>
        /// <remarks>
        /// This is preferable to using <see cref="LogOut()"/>, unless your code is already running from a
        /// background thread.
        /// </remarks>
        public static Task LogOutAsync()
        {
            return LogOutAsync(CancellationToken.None);
        }

        /// <summary>
        /// Logs out the currently logged in user session. This will remove the session from disk, log out of
        /// linked services, and future calls to <see cref="CurrentUser"/> will return <c>null</c>.
        ///
        /// This is preferable to using <see cref="LogOut()"/>, unless your code is already running from a
        /// background thread.
        /// </summary>
        public static Task LogOutAsync(CancellationToken cancellationToken)
        {
            return GetCurrentUserAsync().OnSuccess(t =>
            {
                LogOutWithProviders();

                ParseUser user = t.Result;
                if (user == null)
                {
                    return Task.FromResult(0);
                }

                return user.taskQueue.Enqueue(toAwait => user.LogOutAsync(toAwait, cancellationToken), cancellationToken);
            }).Unwrap();
        }

        internal Task LogOutAsync(Task toAwait, CancellationToken cancellationToken)
        {
            string oldSessionToken = SessionToken;
            if (oldSessionToken == null)
            {
                return Task.FromResult(0);
            }

            // Cleanup in-memory session.
            MutateState(mutableClone =>
            {
                mutableClone.ServerData.Remove("sessionToken");
            });
            var revokeSessionTask = ParseSession.RevokeAsync(oldSessionToken, cancellationToken);
            return Task.WhenAll(revokeSessionTask, CurrentUserController.LogOutAsync(cancellationToken));
        }

        private static void LogOutWithProviders()
        {
            foreach (var provider in authProviders.Values)
            {
                provider.Deauthenticate();
            }
        }

        /// <summary>
        /// Gets the currently logged in ParseUser with a valid session, either from memory or disk
        /// if necessary.
        /// </summary>
        public static ParseUser CurrentUser
        {
            get
            {
                var userTask = GetCurrentUserAsync();
                // TODO (hallucinogen): this will without a doubt fail in Unity. How should we fix it?
                userTask.Wait();
                return userTask.Result;
            }
        }

        /// <summary>
        /// Gets the currently logged in ParseUser with a valid session, either from memory or disk
        /// if necessary, asynchronously.
        /// </summary>
        internal static Task<ParseUser> GetCurrentUserAsync()
        {
            return GetCurrentUserAsync(CancellationToken.None);
        }

        /// <summary>
        /// Gets the currently logged in ParseUser with a valid session, either from memory or disk
        /// if necessary, asynchronously.
        /// </summary>
        internal static Task<ParseUser> GetCurrentUserAsync(CancellationToken cancellationToken)
        {
            return CurrentUserController.GetAsync(cancellationToken);
        }

        private static Task SaveCurrentUserAsync(ParseUser user)
        {
            return SaveCurrentUserAsync(user, CancellationToken.None);
        }

        private static Task SaveCurrentUserAsync(ParseUser user, CancellationToken cancellationToken)
        {
            return CurrentUserController.SetAsync(user, cancellationToken);
        }

        internal static void ClearInMemoryUser()
        {
            CurrentUserController.ClearFromMemory();
        }

        /// <summary>
        /// Constructs a <see cref="ParseQuery{ParseUser}"/> for ParseUsers.
        /// </summary>
        public static ParseQuery<ParseUser> Query
        {
            get
            {
                return new ParseQuery<ParseUser>();
            }
        }

        #region Legacy / Revocable Session Tokens

        private static readonly object isRevocableSessionEnabledMutex = new object();
        private static bool isRevocableSessionEnabled;

        /// <summary>
        /// Tells server to use revocable session on LogIn and SignUp, even when App's Settings
        /// has "Require Revocable Session" turned off. Issues network request in background to
        /// migrate the sessionToken on disk to revocable session.
        /// </summary>
        /// <returns>The Task that upgrades the session.</returns>
        public static Task EnableRevocableSessionAsync()
        {
            return EnableRevocableSessionAsync(CancellationToken.None);
        }

        /// <summary>
        /// Tells server to use revocable session on LogIn and SignUp, even when App's Settings
        /// has "Require Revocable Session" turned off. Issues network request in background to
        /// migrate the sessionToken on disk to revocable session.
        /// </summary>
        /// <returns>The Task that upgrades the session.</returns>
        public static Task EnableRevocableSessionAsync(CancellationToken cancellationToken)
        {
            lock (isRevocableSessionEnabledMutex)
            {
                isRevocableSessionEnabled = true;
            }

            return GetCurrentUserAsync(cancellationToken).OnSuccess(t =>
            {
                var user = t.Result;
                return user.UpgradeToRevocableSessionAsync(cancellationToken);
            });
        }

        internal static void DisableRevocableSession()
        {
            lock (isRevocableSessionEnabledMutex)
            {
                isRevocableSessionEnabled = false;
            }
        }

        internal static bool IsRevocableSessionEnabled
        {
            get
            {
                lock (isRevocableSessionEnabledMutex)
                {
                    return isRevocableSessionEnabled;
                }
            }
        }

        internal Task UpgradeToRevocableSessionAsync()
        {
            return UpgradeToRevocableSessionAsync(CancellationToken.None);
        }

        internal Task UpgradeToRevocableSessionAsync(CancellationToken cancellationToken)
        {
            return taskQueue.Enqueue(toAwait => UpgradeToRevocableSessionAsync(toAwait, cancellationToken),
                cancellationToken);
        }

        internal Task UpgradeToRevocableSessionAsync(Task toAwait, CancellationToken cancellationToken)
        {
            string sessionToken = SessionToken;

            return toAwait.OnSuccess(_ =>
            {
                return ParseSession.UpgradeToRevocableSessionAsync(sessionToken, cancellationToken);
            }).Unwrap().OnSuccess(t =>
            {
                return SetSessionTokenAsync(t.Result);
            }).Unwrap();
        }

        #endregion

        /// <summary>
        /// Requests a password reset email to be sent to the specified email address associated with the
        /// user account. This email allows the user to securely reset their password on the Parse site.
        /// </summary>
        /// <param name="email">The email address associated with the user that forgot their password.</param>
        public static Task RequestPasswordResetAsync(string email)
        {
            return RequestPasswordResetAsync(email, CancellationToken.None);
        }

        /// <summary>
        /// Requests a password reset email to be sent to the specified email address associated with the
        /// user account. This email allows the user to securely reset their password on the Parse site.
        /// </summary>
        /// <param name="email">The email address associated with the user that forgot their password.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static Task RequestPasswordResetAsync(string email,
            CancellationToken cancellationToken)
        {
            return UserController.RequestPasswordResetAsync(email, cancellationToken);
        }

        /// <summary>
        /// Gets the authData for this user.
        /// </summary>
        internal IDictionary<string, IDictionary<string, object>> AuthData
        {
            get
            {
                IDictionary<string, IDictionary<string, object>> authData;
                if (this.TryGetValue<IDictionary<string, IDictionary<string, object>>>(
                    "authData", out authData))
                {
                    return authData;
                }
                return null;
            }
            private set
            {
                this["authData"] = value;
            }
        }

        private static IParseAuthenticationProvider GetProvider(string providerName)
        {
            IParseAuthenticationProvider provider;
            if (authProviders.TryGetValue(providerName, out provider))
            {
                return provider;
            }
            return null;
        }

        /// <summary>
        /// Removes null values from authData (which exist temporarily for unlinking)
        /// </summary>
        private void CleanupAuthData()
        {
            lock (mutex)
            {
                if (!CurrentUserController.IsCurrent(this))
                {
                    return;
                }
                var authData = AuthData;

                if (authData == null)
                {
                    return;
                }

                foreach (var pair in new Dictionary<string, IDictionary<string, object>>(authData))
                {
                    if (pair.Value == null)
                    {
                        authData.Remove(pair.Key);
                    }
                }
            }
        }

        /// <summary>
        /// Synchronizes authData for all providers.
        /// </summary>
        private void SynchronizeAllAuthData()
        {
            lock (mutex)
            {
                var authData = AuthData;

                if (authData == null)
                {
                    return;
                }

                foreach (var pair in authData)
                {
                    SynchronizeAuthData(GetProvider(pair.Key));
                }
            }
        }

        private void SynchronizeAuthData(IParseAuthenticationProvider provider)
        {
            bool restorationSuccess = false;
            lock (mutex)
            {
                var authData = AuthData;
                if (authData == null || provider == null)
                {
                    return;
                }
                IDictionary<string, object> data;
                if (authData.TryGetValue(provider.AuthType, out data))
                {
                    restorationSuccess = provider.RestoreAuthentication(data);
                }
            }

            if (!restorationSuccess)
            {
                this.UnlinkFromAsync(provider.AuthType, CancellationToken.None);
            }
        }

        internal Task LinkWithAsync(string authType, IDictionary<string, object> data, CancellationToken cancellationToken)
        {
            return taskQueue.Enqueue(toAwait =>
            {
                var authData = AuthData;
                if (authData == null)
                {
                    authData = AuthData = new Dictionary<string, IDictionary<string, object>>();
                }
                authData[authType] = data;
                AuthData = authData;
                return SaveAsync(cancellationToken);
            }, cancellationToken);
        }

        internal Task LinkWithAsync(string authType, CancellationToken cancellationToken)
        {
            var provider = GetProvider(authType);
            return provider.AuthenticateAsync(cancellationToken)
              .OnSuccess(t => LinkWithAsync(authType, t.Result, cancellationToken))
              .Unwrap();
        }

        /// <summary>
        /// Unlinks a user from a service.
        /// </summary>
        internal Task UnlinkFromAsync(string authType, CancellationToken cancellationToken)
        {
            return LinkWithAsync(authType, null, cancellationToken);
        }

        /// <summary>
        /// Checks whether a user is linked to a service.
        /// </summary>
        internal bool IsLinked(string authType)
        {
            lock (mutex)
            {
                return AuthData != null && AuthData.ContainsKey(authType) && AuthData[authType] != null;
            }
        }

        internal static Task<ParseUser> LogInWithAsync(string authType,
            IDictionary<string, object> data,
            CancellationToken cancellationToken)
        {
            ParseUser user = null;

            return UserController.LogInAsync(authType, data, cancellationToken).OnSuccess(t =>
            {
                user = ParseObject.FromState<ParseUser>(t.Result, "_User");

                lock (user.mutex)
                {
                    if (user.AuthData == null)
                    {
                        user.AuthData = new Dictionary<string, IDictionary<string, object>>();
                    }
                    user.AuthData[authType] = data;
                    user.SynchronizeAllAuthData();
                }

                return SaveCurrentUserAsync(user);
            }).Unwrap().OnSuccess(t => user);
        }

        internal static Task<ParseUser> LogInWithAsync(string authType,
            CancellationToken cancellationToken)
        {
            var provider = GetProvider(authType);
            return provider.AuthenticateAsync(cancellationToken)
              .OnSuccess(authData => LogInWithAsync(authType, authData.Result, cancellationToken))
              .Unwrap();
        }

        internal static void RegisterProvider(IParseAuthenticationProvider provider)
        {
            authProviders[provider.AuthType] = provider;
            var curUser = ParseUser.CurrentUser;
            if (curUser != null)
            {
                curUser.SynchronizeAuthData(provider);
            }
        }
    }
}
