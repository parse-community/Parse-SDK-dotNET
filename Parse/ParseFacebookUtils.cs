// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using Parse.Internal;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Parse {
  /// <summary>
  /// Provides a set of utilities for using Parse with Facebook.
  /// </summary>
  public static partial class ParseFacebookUtils {
    private static readonly FacebookAuthenticationProvider authProvider =
        new FacebookAuthenticationProvider();

#if !UNITY
    /// <summary>
    /// Gets the Facebook Application ID as supplied to <see cref="ParseFacebookUtils.Initialize"/>
    /// </summary>
    public static string ApplicationId {
      get {
        return authProvider.AppId;
      }
    }
#endif

    /// <summary>
    /// Gets the access token for the currently logged in Facebook user. This can be used with a
    /// Facebook SDK to get access to Facebook user data.
    /// </summary>
    public static string AccessToken {
      get {
        return authProvider.AccessToken;
      }
    }

#if !UNITY
    /// <summary>
    /// Initializes Facebook for use with Parse.
    /// </summary>
    /// <param name="applicationId">Your Facebook application ID.</param>
    public static void Initialize(string applicationId) {
      authProvider.AppId = applicationId;
      ParseUser.RegisterProvider(authProvider);
    }
#else
    /// <summary>
    /// Unity will just auto-initialize this. Since we're not responsible for login, we don't
    /// need the application id -- just the tokens.
    /// </summary>
    internal static void Initialize() {
      ParseUser.RegisterProvider(authProvider);
    }
#endif

    /// <summary>
    /// Logs in a <see cref="ParseUser" /> using Facebook for authentication. If a user for the
    /// given Facebook credentials does not already exist, a new user will be created.
    /// </summary>
    /// <param name="facebookId">The user's Facebook ID.</param>
    /// <param name="accessToken">A valid access token for the user.</param>
    /// <param name="expiration">The expiration date of the access token.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user that was either logged in or created.</returns>
    public static Task<ParseUser> LogInAsync(string facebookId,
        string accessToken,
        DateTime expiration,
        CancellationToken cancellationToken) {
      return ParseUser.LogInWithAsync("facebook",
          authProvider.GetAuthData(facebookId, accessToken, expiration),
          cancellationToken);
    }

    /// <summary>
    /// Logs in a <see cref="ParseUser" /> using Facebook for authentication. If a user for the
    /// given Facebook credentials does not already exist, a new user will be created.
    /// </summary>
    /// <param name="facebookId">The user's Facebook ID.</param>
    /// <param name="accessToken">A valid access token for the user.</param>
    /// <param name="expiration">The expiration date of the access token.</param>
    /// <returns>The user that was either logged in or created.</returns>
    public static Task<ParseUser> LogInAsync(string facebookId,
        string accessToken,
        DateTime expiration) {
      return LogInAsync(facebookId, accessToken, expiration, CancellationToken.None);
    }

    /// <summary>
    /// Links a <see cref="ParseUser" /> to a Facebook account, allowing you to use Facebook
    /// for authentication, and providing access to Facebook data for the user.
    /// </summary>
    /// <param name="user">The user to link to a Facebook account.</param>
    /// <param name="facebookId">The user's Facebook ID.</param>
    /// <param name="accessToken">A valid access token for the user.</param>
    /// <param name="expiration">The expiration date of the access token.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static Task LinkAsync(ParseUser user,
        string facebookId,
        string accessToken,
        DateTime expiration,
        CancellationToken cancellationToken) {
      return user.LinkWithAsync("facebook",
          authProvider.GetAuthData(facebookId, accessToken, expiration),
          cancellationToken);
    }

    /// <summary>
    /// Links a <see cref="ParseUser" /> to a Facebook account, allowing you to use Facebook
    /// for authentication, and providing access to Facebook data for the user.
    /// </summary>
    /// <param name="user">The user to link to a Facebook account.</param>
    /// <param name="facebookId">The user's Facebook ID.</param>
    /// <param name="accessToken">A valid access token for the user.</param>
    /// <param name="expiration">The expiration date of the access token.</param>
    public static Task LinkAsync(ParseUser user,
        string facebookId,
        string accessToken,
        DateTime expiration) {
      return LinkAsync(user, facebookId, accessToken, expiration, CancellationToken.None);
    }

    /// <summary>
    /// Gets whether the given user is linked to a Facebook account. This can only be used on
    /// the currently authorized user.
    /// </summary>
    /// <param name="user">The user to check.</param>
    /// <returns><c>true</c> if the user is linked to a Facebook account.</returns>
    public static bool IsLinked(ParseUser user) {
      return user.IsLinked("facebook");
    }

    /// <summary>
    /// Unlinks a user from a Facebook account. Unlinking a user will save the user's data.
    /// </summary>
    /// <param name="user">The user to unlink.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static Task UnlinkAsync(ParseUser user, CancellationToken cancellationToken) {
      return user.UnlinkFromAsync("facebook", cancellationToken);
    }

    /// <summary>
    /// Unlinks a user from a Facebook account. Unlinking a user will save the user's data.
    /// </summary>
    /// <param name="user">The user to unlink.</param>
    public static Task UnlinkAsync(ParseUser user) {
      return UnlinkAsync(user, CancellationToken.None);
    }
  }
}
