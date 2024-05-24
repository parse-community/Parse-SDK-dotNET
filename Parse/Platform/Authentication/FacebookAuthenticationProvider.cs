using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Authentication;
using Parse.Infrastructure;
using Parse.Infrastructure.Execution;
using Parse.Infrastructure.Utilities;

namespace Parse.Platform.Authentication
{
    /// <summary>
    /// Provides an authenticator service for the Parse SDK which uses Facebook to authorize linked Parse user access.
    /// </summary>
    public class FacebookAuthenticationProvider : IParseAuthenticationProvider, IServiceHubMutator
    {
        static Uri TokenExtensionRoute { get; } = new Uri("https://graph.facebook.com/oauth/access_token", UriKind.Absolute);

        static Uri ProfileRoute { get; } = new Uri("https://graph.facebook.com/me", UriKind.Absolute);

        TaskCompletionSource<IDictionary<string, object>> SubmissionTaskSource { get; set; }

        CancellationToken SubmissionCancellationToken { get; set; }

        /// <summary>
        /// Instantiates a <see cref="FacebookAuthenticationProvider"/>.
        /// </summary>
        public FacebookAuthenticationProvider()
        {
        }

        internal Uri AuthenticationRoute { get; set; } = new Uri("https://www.facebook.com/dialog/oauth", UriKind.Absolute);

        internal Uri SuccessRoute { get; set; } = new Uri("https://www.facebook.com/connect/login_success.html", UriKind.Absolute);

        /// <summary>
        /// Permissions the <see cref="FacebookAuthenticationProvider"/> should ask for from Facebook, in terms of access to or mutation of user data.
        /// </summary>
        public IEnumerable<string> Permissions { get; set; }

        /// <summary>
        /// The identifier to make API calls to Facebook with. This is in the Facebook Developer Dashboard as some variant of "Application ID" or "API Key".
        /// </summary>
        public string Caller { get; set; }

        /// <summary>
        /// The token for access to an individual Facebook user account.
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// An event for when a Facebook-authentication-related web resource is loaded.
        /// </summary>
        public event Action<Uri> Load;

        /// <summary>
        /// Parses a uri, looking for a base uri that represents facebook login completion, and then
        /// converting the query string into a dictionary of key-value pairs. (e.g. access_token)
        /// </summary>
        bool CheckIsOAuthCallback(Uri uri, out IDictionary<string, string> result)
        {
            if (!uri.AbsoluteUri.StartsWith(SuccessRoute.AbsoluteUri) || uri.Fragment == null)
            {
                result = default;
                return default;
            }

            string fragmentOrQuery = uri.Fragment is null or { Length: 0 } ? uri.Query : uri.Fragment;
            result = WebUtilities.DecodeQueryString(fragmentOrQuery.Substring(1));

            return true;
        }

        public IDictionary<string, object> GetAuthenticationData(string facebookId, string accessToken, DateTime expiration) => new Dictionary<string, object>
        {
            ["id"] = facebookId,
            ["access_token"] = accessToken,
            ["expiration_date"] = expiration.ToString(ParseClient.DateFormatStrings[0])
        };

        public bool ExtractUser(IServiceHub serviceHub, Uri oAuthCallback)
        {
            if (CheckIsOAuthCallback(oAuthCallback, out IDictionary<string, string> result))
            {
                void GetUser()
                {
                    try
                    {
                        if (result.ContainsKey("error"))
                        {
                            SubmissionTaskSource.TrySetException(new ParseFailureException(ParseFailureException.ErrorCode.OtherCause, $"{result["error_description"]}: {result["error"]}"));
                            return;
                        }

                        serviceHub.WebClient.ExecuteAsync(new WebRequest { Resource = $"{ProfileRoute}?{WebUtilities.BuildQueryString(new Dictionary<string, object> { ["access_token"] = result["access_token"], ["fields"] = "id" })}", Method = "GET" }, default, default, CancellationToken.None).OnSuccess(task => SubmissionTaskSource.TrySetResult(GetAuthenticationData(JsonUtilities.DeserializeJsonText(task.Result.Item2)["id"] as string, result["access_token"], DateTime.Now + TimeSpan.FromSeconds(Int32.Parse(result["expires_in"]))))).ContinueWith(task =>
                        {
                            if (task.IsFaulted)
                            {
                                SubmissionTaskSource.TrySetException(task.Exception);
                            }
                        });
                    }
                    catch (Exception e)
                    {
                        SubmissionTaskSource.TrySetException(e);
                    }
                }

                GetUser();
                return true;
            }

            return default;
        }

        /// <inheritdoc/>
        public Task<IDictionary<string, object>> AuthenticateAsync(CancellationToken cancellationToken)
        {
            if (Caller is null)
            {
                throw new InvalidOperationException("You must initialize FacebookUtilities or provide a FacebookAuthenticationMutator instance to the ParseClient customization constructor before attempting a Facebook login.");
            }

            if (SubmissionTaskSource != null)
            {
                SubmissionTaskSource.TrySetCanceled();
            }

            TaskCompletionSource<IDictionary<string, object>> tcs = new TaskCompletionSource<IDictionary<string, object>> { };

            SubmissionCancellationToken = cancellationToken;
            SubmissionTaskSource = tcs;

            cancellationToken.Register(() => tcs.TrySetCanceled());

            Action<Uri> navigateHandler = Load;

            if (navigateHandler != null)
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>()
                {
                    ["redirect_uri"] = SuccessRoute.AbsoluteUri,
                    ["response_type"] = "token",
                    ["display"] = "popup",
                    ["client_id"] = Caller
                };

                if (Permissions != null)
                {
                    parameters["scope"] = String.Join(",", Permissions.ToArray());
                }

                navigateHandler(new Uri(AuthenticationRoute, $"?{WebUtilities.BuildQueryString(parameters)}"));
            }
            return tcs.Task;
        }

        /// <inheritdoc/>
        public void Deauthenticate() => AccessToken = default;

        /// <inheritdoc/>
        public bool RestoreAuthentication(IDictionary<string, object> authData)
        {
            if (authData == null)
            {
                Deauthenticate();
            }
            else
            {
                AccessToken = authData["access_token"] as string;
            }

            return true;
        }

        /// <inheritdoc/>
        public string Name => "facebook";

        void IServiceHubMutator.Mutate(ref IMutableServiceHub mutableHub, in IServiceHub consumableHub, Stack<IServiceHubMutator> futureMutators) => mutableHub.InitializeFacebookAuthentication(authenticator: this);

        bool IServiceHubMutator.Valid => Caller is { Length: > 0 };
    }
}
