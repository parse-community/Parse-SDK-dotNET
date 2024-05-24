using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Parse.Abstractions.Infrastructure;
using Parse.Platform.Authentication;

namespace Parse.Infrastructure.Utilities
{
    // TODO: Add convenience methods for individual environments back.

    /// <summary>
    /// Provides a set of utilities for the use of Facebook as an authenticator for Parse.
    /// </summary>
    public static partial class FacebookAuthenticationUtilities
    {
        static FacebookAuthenticationProvider Provider { get; set; } = new FacebookAuthenticationProvider { };

        /// <summary>
        /// Gets the Facebook Application ID as supplied to <see cref="InitializeFacebookAuthenticationProvider"/>.
        /// </summary>
        public static string Identifier => Provider.Caller;

        /// <summary>
        /// Gets the access token for the currently logged in Facebook user. This can be used with a
        /// Facebook SDK to get access to Facebook user data.
        /// </summary>
        public static string AccessToken => Provider.AccessToken;

        /// <summary>
        /// Initializes Facebook for use with Parse.
        /// </summary>
        /// <param name="serviceHub">The service hub to use.</param>
        /// <param name="identifier">Your Facebook application ID.</param>
        public static void InitializeFacebookAuthenticationProvider(this IServiceHub serviceHub, string identifier)
        {
            Provider.Caller = identifier;
            serviceHub.SetAuthenticationProvider(Provider);
        }

        /// <summary>
        /// Initializes Facebook for use with Parse.
        /// </summary>
        /// <param name="serviceHub">The service hub to use.</param>
        /// <param name="authenticator">The <see cref="FacebookAuthenticationProvider"/> instance to use for Facebook authentication.</param>
        public static void InitializeFacebookAuthentication(this IServiceHub serviceHub, FacebookAuthenticationProvider authenticator)
        {
            if (authenticator is IServiceHubMutator { Valid: true })
            {
                serviceHub.SetAuthenticationProvider(Provider = authenticator);
            }
        }

        /// <summary>
        /// Logs in a <see cref="ParseUser" /> using Facebook for authentication. If a user for the
        /// given Facebook credentials does not already exist, a new user will be created.
        /// </summary>
        /// <param name="serviceHub">The service hub to use.</param>
        /// <param name="facebookId">The user's Facebook ID.</param>
        /// <param name="accessToken">A valid access token for the user.</param>
        /// <param name="expiration">The expiration date of the access token.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The user that was either logged in or created.</returns>
        public static Task<ParseUser> AuthenticateWithFacebookAsync(this IServiceHub serviceHub, string facebookId, string accessToken, DateTime expiration, CancellationToken cancellationToken = default) => serviceHub.AuthenticateWithServiceAsync("facebook", Provider.GetAuthenticationData(facebookId, accessToken, expiration), cancellationToken);

        /// <summary>
        /// Links a <see cref="ParseUser" /> to a Facebook account, allowing you to use Facebook
        /// for authentication, and providing access to Facebook data for the user.
        /// </summary>
        /// <param name="user">The user to link to a Facebook account.</param>
        /// <param name="facebookId">The user's Facebook ID.</param>
        /// <param name="accessToken">A valid access token for the user.</param>
        /// <param name="expiration">The expiration date of the access token.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static Task LinkToFacebookAsync(this ParseUser user, string facebookId, string accessToken, DateTime expiration, CancellationToken cancellationToken = default) => user.LinkToServiceAsync("facebook", Provider.GetAuthenticationData(facebookId, accessToken, expiration), cancellationToken);

        /// <summary>
        /// Gets whether the given user is linked to a Facebook account. This can only be used on
        /// the currently authorized user.
        /// </summary>
        /// <param name="user">The user to check.</param>
        /// <returns><c>true</c> if the user is linked to a Facebook account.</returns>
        public static bool CheckLinkedToFacebook(this ParseUser user) => user.CheckLinkedToService("facebook");

        /// <summary>
        /// Unlinks a user from a Facebook account. Unlinking a user will save the user's data.
        /// </summary>
        /// <param name="user">The user to unlink.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static Task UnlinkFromFacebookAsync(this ParseUser user, CancellationToken cancellationToken = default) => user.UnlinkFromServiceAsync("facebook", cancellationToken);
    }
}
