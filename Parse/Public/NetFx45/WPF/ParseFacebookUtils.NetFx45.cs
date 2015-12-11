// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Parse {
  public static partial class ParseFacebookUtils {
    /// <summary>
    /// Logs in a <see cref="ParseUser" /> using Facebook for authentication. If a user for the
    /// given Facebook credentials does not already exist, a new user will be created.
    /// 
    /// The user will be logged in through Facebook's OAuth web flow, so you must supply a
    /// <paramref name="webView"/> that will be navigated to Facebook's authentication pages.
    /// </summary>
    /// <param name="webView">A web view that will be used to present the authorization pages
    /// to the user.</param>
    /// <param name="permissions">A list of Facebook permissions to request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user that was either logged in or created.</returns>
    public static async Task<ParseUser> LogInAsync(WebBrowser webView,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken) {
      authProvider.Permissions = permissions;
      LoadCompletedEventHandler loadCompleted = (_, e) => authProvider.HandleNavigation(e.Uri);
      webView.LoadCompleted += loadCompleted;
      Action<Uri> navigate = uri => webView.Navigate(uri);
      authProvider.Navigate += navigate;
      var result = await ParseUser.LogInWithAsync("facebook", cancellationToken);
      authProvider.Navigate -= navigate;
      webView.LoadCompleted -= loadCompleted;
      return result;
    }

    /// <summary>
    /// Links a <see cref="ParseUser" /> to a Facebook account, allowing you to use Facebook
    /// for authentication, and providing access to Facebook data for the user.
    /// 
    /// The user will be logged in through Facebook's OAuth web flow, so you must supply a
    /// <paramref name="webView"/> that will be navigated to Facebook's authentication pages.
    /// </summary>
    /// <param name="user">The user to link with Facebook.</param>
    /// <param name="webView">A web view that will be used to present the authorization pages
    /// to the user.</param>
    /// <param name="permissions">A list of Facebook permissions to request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async Task LinkAsync(ParseUser user,
        WebBrowser webView,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken) {
      authProvider.Permissions = permissions;
      LoadCompletedEventHandler loadCompleted = (_, e) => authProvider.HandleNavigation(e.Uri);
      webView.LoadCompleted += loadCompleted;
      Action<Uri> navigate = uri => webView.Navigate(uri);
      authProvider.Navigate += navigate;
      await user.LinkWithAsync("facebook", cancellationToken);
      authProvider.Navigate -= navigate;
      webView.LoadCompleted -= loadCompleted;
    }

    /// <summary>
    /// Logs in a <see cref="ParseUser" /> using Facebook for authentication. If a user for the
    /// given Facebook credentials does not already exist, a new user will be created.
    /// 
    /// The user will be logged in through Facebook's OAuth web flow, so you must supply a
    /// <paramref name="webView"/> that will be navigated to Facebook's authentication pages.
    /// </summary>
    /// <param name="webView">A web view that will be used to present the authorization pages
    /// to the user.</param>
    /// <param name="permissions">A list of Facebook permissions to request.</param>
    /// <returns>The user that was either logged in or created.</returns>
    public static Task<ParseUser> LogInAsync(WebBrowser webView, IEnumerable<string> permissions) {
      return LogInAsync(webView, permissions, CancellationToken.None);
    }

    /// <summary>
    /// Links a <see cref="ParseUser" /> to a Facebook account, allowing you to use Facebook
    /// for authentication, and providing access to Facebook data for the user.
    /// 
    /// The user will be logged in through Facebook's OAuth web flow, so you must supply a
    /// <paramref name="webView"/> that will be navigated to Facebook's authentication pages.
    /// </summary>
    /// <param name="user">The user to link with Facebook.</param>
    /// <param name="webView">A web view that will be used to present the authorization pages
    /// to the user.</param>
    /// <param name="permissions">A list of Facebook permissions to request.</param>
    public static Task LinkAsync(ParseUser user, WebBrowser webView, IEnumerable<string> permissions) {
      return LinkAsync(user, webView, permissions, CancellationToken.None);
    }
  }
}
