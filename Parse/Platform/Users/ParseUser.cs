using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Abstractions.Platform.Authentication;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure.Utilities;

namespace Parse
{
    /// <summary>
    /// Represents a user for a Parse application.
    /// </summary>
    [ParseClassName("_User")]
    public class ParseUser : ParseObject
    {
        /// <summary>
        /// Indicates whether the current ParseUser has been authenticated on this device. 
        /// Authentication means that the user has a valid session token and has been logged 
        /// into the application.
        /// </summary>
        public bool IsAuthenticated
        {
            get
            {
                // Synchronize access to the critical section to ensure thread safety
                lock (Mutex)
                {
                    // Step 1: Check if the session token exists
                    // The session token is generated when the user logs in successfully
                    // or signs up. If it is null or empty, the user is not authenticated.
                    if (SessionToken == null)
                    {
                        return false; // No session token means the user is not authenticated
                    }

                    // Step 2: Get the current user from the IServiceHub (via the Services instance)
                    // This typically represents the currently logged-in user.
                    var currentUser = Services.GetCurrentUser();

                    // Step 3: Ensure the current user is not null
                    // If no user is retrieved from the service hub, the current ParseUser
                    // cannot be considered authenticated.
                    if (currentUser == null)
                    {
                        return false; // No current user means the user is not authenticated
                    }

                    // Step 4: Compare the current user's ObjectId with this user's ObjectId
                    // If the ObjectIds match, it means this ParseUser is the currently
                    // authenticated user.
                    bool isSameUser = currentUser.ObjectId == ObjectId;
                   
                    // Return the final result of the comparison
                    return isSameUser;
                }
            }
        }


        /// <summary>
        /// Removes a key from the object's data if it exists.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <exception cref="ArgumentException">Cannot remove the username key.</exception>
        public override void Remove(string key)
        {
            if (key == "username")
            {
                throw new InvalidOperationException("Cannot remove the username key.");
            }

            base.Remove(key);
        }

        protected override bool CheckKeyMutable(string key)
        {
            return !ImmutableKeys.Contains(key);
        }

        internal override void HandleSave(IObjectState serverState)
        {
            base.HandleSave(serverState);

            SynchronizeAllAuthData();
            CleanupAuthData();

            MutateState(mutableClone => mutableClone.ServerData.Remove("password"));
        }

        public string SessionToken => State.ContainsKey("sessionToken") ? State["sessionToken"] as string : null;

        internal Task SetSessionTokenAsync(string newSessionToken)
        {
            return SetSessionTokenAsync(newSessionToken, CancellationToken.None);
        }

        internal Task SetSessionTokenAsync(string newSessionToken, CancellationToken cancellationToken)
        {
            MutateState(mutableClone => mutableClone.ServerData["sessionToken"] = newSessionToken);
            return Services.SaveCurrentUserAsync(this);
        }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        [ParseFieldName("username")]
        public string Username
        {
            get => GetProperty<string>(null, nameof(Username));
            set => SetProperty(value, nameof(Username));
        }

        /// <summary>
        /// Sets the password.
        /// </summary>
        [ParseFieldName("password")]
        public string Password
        {
            get => GetProperty<string>(null, nameof(Password));
            set => SetProperty(value, nameof(Password));
        }

        /// <summary>
        /// Sets the email address.
        /// </summary>
        [ParseFieldName("email")]
        public string Email
        {
            get => GetProperty<string>(null, nameof(Email));
            set => SetProperty(value, nameof(Email));
        }

        internal Task SignUpAsync(Task toAwait, CancellationToken cancellationToken)
        {
            if (AuthData == null)
            {
                // TODO (hallucinogen): make an Extension of Task to create Task with exception/canceled.
                if (String.IsNullOrEmpty(Username))
                {
                    TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
                    tcs.TrySetException(new InvalidOperationException("Cannot sign up user with an empty name."));
                    return tcs.Task;
                }
                if (String.IsNullOrEmpty(Password))
                {
                    TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
                    tcs.TrySetException(new InvalidOperationException("Cannot sign up user with an empty password."));
                    return tcs.Task;
                }
            }
            if (!String.IsNullOrEmpty(ObjectId))
            {
                TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
                tcs.TrySetException(new InvalidOperationException("Cannot sign up a user that already exists."));
                return tcs.Task;
            }

            IDictionary<string, IParseFieldOperation> currentOperations = StartSave();

            return toAwait.OnSuccess(_ => Services.UserController.SignUpAsync(State, currentOperations, Services, cancellationToken)).Unwrap().ContinueWith(t =>
            {
                if (t.IsFaulted || t.IsCanceled)
                {
                    HandleFailedSave(currentOperations);
                }
                else
                {
                    HandleSave(t.Result);
                }
                return t;
            }).Unwrap().OnSuccess(_ => Services.SaveCurrentUserAsync(this)).Unwrap();
        }

        /// <summary>
        /// Signs up a new user. This will create a new ParseUser on the server and will also persist the
        /// session on disk so that you can access the user using <see cref="InstanceUser"/>. A username and
        /// password must be set before calling SignUpAsync.
        /// </summary>
        public Task SignUpAsync()
        {
            return SignUpAsync(CancellationToken.None);
        }

        /// <summary>
        /// Signs up a new user. This will create a new ParseUser on the server and will also persist the
        /// session on disk so that you can access the user using <see cref="InstanceUser"/>. A username and
        /// password must be set before calling SignUpAsync.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        public Task SignUpAsync(CancellationToken cancellationToken)
        {
            return TaskQueue.Enqueue(toAwait => SignUpAsync(toAwait, cancellationToken), cancellationToken);
        }

        protected override Task SaveAsync(Task toAwait, CancellationToken cancellationToken)
        {
            lock (Mutex)
            {
                if (ObjectId is null)
                {
                    throw new InvalidOperationException("You must call SignUpAsync before calling SaveAsync.");
                }

                return base.SaveAsync(toAwait, cancellationToken).OnSuccess(_ => Services.CurrentUserController.IsCurrent(this) ? Services.SaveCurrentUserAsync(this) : Task.CompletedTask).Unwrap();
            }
        }

        // If this is already the current user, refresh its state on disk.
        internal override Task<ParseObject> FetchAsyncInternal(Task toAwait, CancellationToken cancellationToken)
        {
            return base.FetchAsyncInternal(toAwait, cancellationToken).OnSuccess(t => !Services.CurrentUserController.IsCurrent(this) ? Task.FromResult(t.Result) : Services.SaveCurrentUserAsync(this).OnSuccess(_ => t.Result)).Unwrap();
        }

        internal Task LogOutAsync(Task toAwait, CancellationToken cancellationToken)
        {
            string oldSessionToken = SessionToken;
            if (oldSessionToken == null)
            {
                return Task.FromResult(0);
            }

            // Cleanup in-memory session.

            MutateState(mutableClone => mutableClone.ServerData.Remove("sessionToken"));
            Task revokeSessionTask = Services.RevokeSessionAsync(oldSessionToken, cancellationToken);
            return Task.WhenAll(revokeSessionTask, Services.CurrentUserController.LogOutAsync(Services, cancellationToken));
        }

        internal Task UpgradeToRevocableSessionAsync()
        {
            return UpgradeToRevocableSessionAsync(CancellationToken.None);
        }

        internal Task UpgradeToRevocableSessionAsync(CancellationToken cancellationToken)
        {
            return TaskQueue.Enqueue(toAwait => UpgradeToRevocableSessionAsync(toAwait, cancellationToken), cancellationToken);
        }

        internal Task UpgradeToRevocableSessionAsync(Task toAwait, CancellationToken cancellationToken)
        {
            string sessionToken = SessionToken;

            return toAwait.OnSuccess(_ => Services.UpgradeToRevocableSessionAsync(sessionToken, cancellationToken)).Unwrap().OnSuccess(task => SetSessionTokenAsync(task.Result)).Unwrap();
        }

        /// <summary>
        /// Gets the authData for this user.
        /// </summary>
        public IDictionary<string, IDictionary<string, object>> AuthData
        {
            get => TryGetValue("authData", out IDictionary<string, IDictionary<string, object>> authData) ? authData : null;
            set => this["authData"] = value;
        }

        /// <summary>
        /// Removes null values from authData (which exist temporarily for unlinking)
        /// </summary>
        void CleanupAuthData()
        {
            lock (Mutex)
            {
                if (!Services.CurrentUserController.IsCurrent(this))
                {
                    return;
                }

                IDictionary<string, IDictionary<string, object>> authData = AuthData;

                if (authData == null)
                {
                    return;
                }

                foreach (KeyValuePair<string, IDictionary<string, object>> pair in new Dictionary<string, IDictionary<string, object>>(authData))
                {
                    if (pair.Value == null)
                    {
                        authData.Remove(pair.Key);
                    }
                }
            }
        }

#pragma warning disable CS1030 // #warning directive
#warning Check if the following properties should be injected via IServiceHub.UserController (except for ImmutableKeys).

        internal static IParseAuthenticationProvider GetProvider(string providerName)
        {
            return Authenticators.TryGetValue(providerName, out IParseAuthenticationProvider provider) ? provider : null;
        }
#pragma warning restore CS1030 // #warning directive

        internal static IDictionary<string, IParseAuthenticationProvider> Authenticators { get; } = new Dictionary<string, IParseAuthenticationProvider> { };

        internal static HashSet<string> ImmutableKeys { get; } = new HashSet<string> { "sessionToken", "isNew" };

        /// <summary>
        /// Synchronizes authData for all providers.
        /// </summary>
        internal void SynchronizeAllAuthData()
        {
            lock (Mutex)
            {
                IDictionary<string, IDictionary<string, object>> authData = AuthData;

                if (authData == null)
                {
                    return;
                }

                foreach (KeyValuePair<string, IDictionary<string, object>> pair in authData)
                {
                    SynchronizeAuthData(GetProvider(pair.Key));
                }
            }
        }

        internal void SynchronizeAuthData(IParseAuthenticationProvider provider)
        {
            bool restorationSuccess = false;

            lock (Mutex)
            {
                IDictionary<string, IDictionary<string, object>> authData = AuthData;

                if (authData == null || provider == null)
                {
                    return;
                }

                if (authData.TryGetValue(provider.AuthType, out IDictionary<string, object> data))
                {
                    restorationSuccess = provider.RestoreAuthentication(data);
                }
            }

            if (!restorationSuccess)
            {
                UnlinkFromAsync(provider.AuthType, CancellationToken.None);
            }
        }

        internal Task LinkWithAsync(string authType, IDictionary<string, object> data, CancellationToken cancellationToken)
        {
            return TaskQueue.Enqueue(toAwait =>
        {
            IDictionary<string, IDictionary<string, object>> authData = AuthData;

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
            IParseAuthenticationProvider provider = GetProvider(authType);
            return provider.AuthenticateAsync(cancellationToken).OnSuccess(t => LinkWithAsync(authType, t.Result, cancellationToken)).Unwrap();
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
            lock (Mutex)
            {
                return AuthData != null && AuthData.ContainsKey(authType) && AuthData[authType] != null;
            }
        }
    }
}
