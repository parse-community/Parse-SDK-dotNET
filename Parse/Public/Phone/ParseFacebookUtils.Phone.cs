// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using System.Xml.Linq;
using System.Linq;
using Parse.Internal;
using Windows.System;
using System.Windows.Controls;

namespace Parse {
  public static partial class ParseFacebookUtils {

    /// <summary>
    /// Fetches the app's ProductID from its manifest.
    /// </summary>
    private static string GetProductId() {
      return (from manifest in XElement.Load("WMAppManifest.xml")
                                       .Descendants("App")
              select manifest).SingleOrDefault().Attribute("ProductID").Value.Trim('{', '}');
    }

    private static string nativeLoginAppPrefix;
    private static string NativeLoginAppPrefix {
      get {
        return nativeLoginAppPrefix = nativeLoginAppPrefix ??
            string.Format("msft-{0}://authorize/", GetProductId().Replace("-", ""));
      }
    }

    private static Uri NativeLoginDialogUrl {
      get {
        return new Uri("fbconnect://authorize");
      }
    }

    private static Uri NativeResponseUrl {
      get {
        return new Uri(NativeLoginAppPrefix);
      }
    }

    /// <summary>
    /// Checks whether the Uri passed into your application comes from the Facebook
    /// app as a result of a completed login attempt.
    /// 
    /// Your code will usually look like this:
    /// <code>
    /// RootFrame.Navigating += async (sender, e) => {
    ///   if (ParseFacebookUtils.IsLoginRedirect(e.Uri)) {
    ///     ParseUser user = await ParseFacebookUtils.EndLoginAsync(
    ///         sender, e, new Uri("/LandingPage.xaml", UriKind.Relative));
    ///     // A new user is now logged in.
    ///   }
    /// };
    /// </code>
    /// </summary>
    /// <param name="uri"></param>
    /// <returns><c>true</c> iff the Uri is a Facebook login redirect, <c>false</c>
    /// otherwise</returns>
    public static bool IsLogInRedirect(Uri uri) {
      if (uri.ToString().StartsWith("/Protocol?")) {
        if (!uri.IsAbsoluteUri) {
          uri = new Uri(new Uri("dummy:///"), uri);
        }
        var queryString = ParseClient.DecodeQueryString(uri.Query.Substring(1));
        var launchUri = new Uri(Uri.UnescapeDataString(queryString["encodedLaunchUri"]));
        if (launchUri.IsAbsoluteUri &&
            launchUri.AbsoluteUri.StartsWith(NativeLoginAppPrefix)) {
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Call this method within your RootFrame.Navigating event handler to complete native Facebook
    /// sign-on. When handling a Facebook login redirect URI, this method will cancel the
    /// pending navigation, begin asynchronously logging in the user, and immediately navigate
    /// to the <paramref name="redirectUri"/>.
    /// 
    /// Your code will usually look like this:
    /// <code>
    /// RootFrame.Navigating += async (sender, e) => {
    ///   if (ParseFacebookUtils.IsLoginRedirect(e.Uri)) {
    ///     ParseUser user = await ParseFacebookUtils.EndLoginAsync(
    ///         sender, e, new Uri("/LandingPage.xaml", UriKind.Relative));
    ///     // A new user is now logged in.
    ///   }
    /// };
    /// </code>
    /// </summary>
    /// <param name="sender">The sender for the Navigating event.</param>
    /// <param name="e">The Navigating event args.</param>
    /// <param name="redirectUri">The Uri within your app to redirect to.</param>
    /// <returns>The ParseUser created or logged in using Facebook credentials, or null if
    /// this was not a Facebook login redirect.</returns>
    public async static Task<ParseUser> EndLogInAsync(object sender,
        NavigatingCancelEventArgs e,
        Uri redirectUri) {
      if (!IsLogInRedirect(e.Uri)) {
        return null;
      }
      authProvider.ResponseUrlOverride = NativeResponseUrl;
      Action<Uri> navigate = (_) => { };
      authProvider.Navigate += navigate;
      try {
        var cts = new CancellationTokenSource();
        // Kicks off a dummy to restart authentication.
        var result = ParseUser.LogInWithAsync("facebook", cts.Token);
        // Complete the authentication.
        var uri = new Uri(new Uri("dummy:///"), e.Uri);
        var queryString = ParseClient.DecodeQueryString(uri.Query.Substring(1));
        var launchUri = new Uri(Uri.UnescapeDataString(queryString["encodedLaunchUri"]));
        if (!authProvider.HandleNavigation(launchUri)) {
          // Cancel the pending attempt to log in if for some reason this wasn't actually
          // a facebook login navigation (unlikely at this point)
          cts.Cancel();
        }

        // Cancel navigation and redirect to the new Uri.
        e.Cancel = true;
        var navService = sender as NavigationService;
        if (navService == null) {
          throw new ArgumentException("sender must be a NavigationService", "sender");
        }
        // Welcome to Windows Phone... sometimes you just have to dispatch for no
        // particularly good reason.
        Deployment.Current.Dispatcher.BeginInvoke(() => navService.Navigate(redirectUri));
        return await result;
      } finally {
        authProvider.Navigate -= navigate;
        authProvider.ResponseUrlOverride = null;
      }
    }

    /// <summary>
    /// Logs in a <see cref="ParseUser" /> using Facebook for authentication. If a user for the
    /// given Facebook credentials does not already exist, a new user will be created.
    /// 
    /// The user will be logged in through the Facebook app's single sign-on mechanism.
    /// 
    /// You must add a handler to your RootFrame's Navigating event that calls EndLogInAsync so
    /// that ParseFacebookUtils can handle incoming navigation attempts.
    /// </summary>=
    /// <param name="permissions">A list of Facebook permissions to request.</param>
    public static void BeginLogIn(IEnumerable<string> permissions) {
      authProvider.Permissions = permissions;
      authProvider.LoginDialogUrlOverride = NativeLoginDialogUrl;
      authProvider.ResponseUrlOverride = NativeResponseUrl;
      Action<Uri> navigate = async (uri) => await Launcher.LaunchUriAsync(uri);
      authProvider.Navigate += navigate;
      try {
        // Kicks off the authentication. This will probably kill the process.
        var _ = authProvider.AuthenticateAsync(CancellationToken.None);
      } finally {
        authProvider.Navigate -= navigate;
        authProvider.LoginDialogUrlOverride = null;
        authProvider.ResponseUrlOverride = null;
      }
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
    /// Logs in a <see cref="ParseUser" /> using Facebook for authentication . If a user for the
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
