using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Abstractions.Platform.Authentication;
using Parse.Abstractions.Platform.Objects;

namespace Parse
{
    [ParseClassName("_User")]
    public class ParseUser : ParseObject
    {
        public bool IsAuthenticated
        {
            get
            {
                lock (Mutex)
                {
                    if (SessionToken == null)
                        return false;

                    var currentUser = Services.GetCurrentUser();
                    return currentUser?.ObjectId == ObjectId;
                }
            }
        }

        public override void Remove(string key)
        {
            if (key == "username")
                throw new InvalidOperationException("Cannot remove the username key.");

            base.Remove(key);
        }

        protected override bool CheckKeyMutable(string key) => !ImmutableKeys.Contains(key);

        internal override void HandleSave(IObjectState serverState)
        {
            base.HandleSave(serverState);
            SynchronizeAllAuthData();
            CleanupAuthData();
            MutateState(mutableClone => mutableClone.ServerData.Remove("password"));
        }

        public string SessionToken => State.ContainsKey("sessionToken") ? State["sessionToken"] as string : null;


        internal async Task SetSessionTokenAsync(string newSessionToken, CancellationToken cancellationToken = default)
        {
            MutateState(mutableClone => mutableClone.ServerData["sessionToken"] = newSessionToken);
            await Services.SaveCurrentUserAsync(this, cancellationToken).ConfigureAwait(false);
        }

        [ParseFieldName("username")]
        public string Username
        {
            get => GetProperty<string>(null, nameof(Username));
            set => SetProperty(value, nameof(Username));
        }

        [ParseFieldName("password")]
        public string Password
        {
            get => GetProperty<string>(null, nameof(Password));
            set => SetProperty(value, nameof(Password));
        }

        [ParseFieldName("email")]
        public string Email
        {
            get => GetProperty<string>(null, nameof(Email));
            set => SetProperty(value, nameof(Email));
        }

        internal async Task SignUpAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(Username))
                throw new InvalidOperationException("Cannot sign up user with an empty name.");

            if (string.IsNullOrWhiteSpace(Password))
                throw new InvalidOperationException("Cannot sign up user with an empty password.");

            if (!string.IsNullOrWhiteSpace(ObjectId))
                throw new InvalidOperationException("Cannot sign up a user that already exists.");

            var currentOperations = StartSave();

            try
            {
                var result = await Services.UserController.SignUpAsync(State, currentOperations, Services, cancellationToken).ConfigureAwait(false);
                HandleSave(result);
                await Services.SaveCurrentUserAsync(this, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                HandleFailedSave(currentOperations);
                throw;
            }
        }

        protected override async Task SaveAsync(Task toAwait, CancellationToken cancellationToken)
        {
            await toAwait.ConfigureAwait(false);

            if (ObjectId is null)
                throw new InvalidOperationException("You must call SignUpAsync before calling SaveAsync.");

            await base.SaveAsync(toAwait, cancellationToken).ConfigureAwait(false);

            if (Services.CurrentUserController.IsCurrent(this))
            {
                await Services.SaveCurrentUserAsync(this, cancellationToken).ConfigureAwait(false);
            }
        }

        internal override async Task<ParseObject> FetchAsyncInternal(CancellationToken cancellationToken)
        {
            //await toAwait.ConfigureAwait(false);

            var result = await base.FetchAsyncInternal(cancellationToken).ConfigureAwait(false);

            if (Services.CurrentUserController.IsCurrent(this))
            {
                await Services.SaveCurrentUserAsync(this, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }

        internal async Task LogOutAsync(CancellationToken cancellationToken)
        {
            var oldSessionToken = SessionToken;
            if (oldSessionToken == null)
                return;

            MutateState(mutableClone => mutableClone.ServerData.Remove("sessionToken"));

            await Task.WhenAll(
                Services.RevokeSessionAsync(oldSessionToken, cancellationToken),
                Services.CurrentUserController.LogOutAsync(Services, cancellationToken)
            ).ConfigureAwait(false);
        }

        internal async Task UpgradeToRevocableSessionAsync(CancellationToken cancellationToken = default)
        {
            var sessionToken = SessionToken;
            var newSessionToken = await Services.UpgradeToRevocableSessionAsync(sessionToken, cancellationToken).ConfigureAwait(false);
            await SetSessionTokenAsync(newSessionToken, cancellationToken).ConfigureAwait(false);
        }
        //public string SessionToken => State.ContainsKey("sessionToken") ? State["sessionToken"] as string : null;

        public IDictionary<string, IDictionary<string, object>> AuthData
        {

            get => ContainsKey("authData") ? AuthData["authData"] as IDictionary<string, IDictionary<string, object>> : null;
            set => this["authData"] = value;
        }

        void CleanupAuthData()
        {
            lock (Mutex)
            {
                if (!Services.CurrentUserController.IsCurrent(this))
                    return;

                var authData = AuthData;
                if (authData == null)
                    return;

                foreach (var key in new List<string>(authData.Keys))
                {
                    if (authData[key] == null)
                    {
                        authData.Remove(key);
                    }
                }
            }
        }

        internal async Task LinkWithAsync(string authType, IDictionary<string, object> data, CancellationToken cancellationToken)
        {
            lock (Mutex)
            {
                AuthData ??= new Dictionary<string, IDictionary<string, object>>();
                AuthData[authType] = data;
            }

            await SaveAsync(cancellationToken).ConfigureAwait(false);
        }

        internal async Task LinkWithAsync(string authType, CancellationToken cancellationToken)
        {
            var provider = GetProvider(authType);
            if (provider != null)
            {
                var authData = await provider.AuthenticateAsync(cancellationToken).ConfigureAwait(false);
                await LinkWithAsync(authType, authData, cancellationToken).ConfigureAwait(false);
            }
        }

        internal Task UnlinkFromAsync(string authType, CancellationToken cancellationToken)
        {
            return LinkWithAsync(authType, null, cancellationToken);
        }

        internal bool IsLinked(string authType)
        {
            lock (Mutex)
            {
                return AuthData != null && AuthData.TryGetValue(authType, out var data) && data != null;
            }
        }

        internal static IParseAuthenticationProvider GetProvider(string providerName)
        {
            return Authenticators.TryGetValue(providerName, out var provider) ? provider : null;
        }

        internal static IDictionary<string, IParseAuthenticationProvider> Authenticators { get; } = new Dictionary<string, IParseAuthenticationProvider>();
        internal static HashSet<string> ImmutableKeys { get; } = new() { "sessionToken", "isNew" };

        internal void SynchronizeAllAuthData()
        {
            lock (Mutex)
            {
                var authData = AuthData;
                if (authData == null)
                    return;

                foreach (var provider in authData.Keys)
                {
                    SynchronizeAuthData(GetProvider(provider));
                }
            }
        }

        internal void SynchronizeAuthData(IParseAuthenticationProvider provider)
        {
            if (provider == null || AuthData == null)
                return;

            if (!AuthData.TryGetValue(provider.AuthType, out var data))
                return;

            if (!provider.RestoreAuthentication(data))
            {
                UnlinkFromAsync(provider.AuthType, CancellationToken.None);
            }
        }
    }
}
