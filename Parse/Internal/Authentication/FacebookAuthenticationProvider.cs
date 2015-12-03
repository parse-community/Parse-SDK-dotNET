// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Parse.Internal {
  class FacebookAuthenticationProvider : IParseAuthenticationProvider {
    internal static readonly Uri LoginDialogUrl =
        new Uri("https://www.facebook.com/dialog/oauth", UriKind.Absolute);
    private static readonly Uri TokenExtensionUrl =
        new Uri("https://graph.facebook.com/oauth/access_token", UriKind.Absolute);
    internal static readonly Uri ResponseUrl =
        new Uri("https://www.facebook.com/connect/login_success.html", UriKind.Absolute);
    private static readonly Uri MeUrl =
        new Uri("https://graph.facebook.com/me", UriKind.Absolute);
    private TaskCompletionSource<IDictionary<string, object>> pendingTask;
    private CancellationToken pendingCancellationToken;

    public FacebookAuthenticationProvider() {
    }

    internal Uri LoginDialogUrlOverride { get; set; }
    internal Uri ResponseUrlOverride { get; set; }

    public IEnumerable<string> Permissions { get; set; }
    public string AppId { get; set; }
    public string AccessToken { get; set; }

    public event Action<Uri> Navigate;

    /// <summary>
    /// Parses a uri, looking for a base uri that represents facebook login completion, and then
    /// converting the query string into a dictionary of key-value pairs. (e.g. access_token)
    /// </summary>
    private bool TryParseOAuthCallbackUrl(Uri uri, out IDictionary<string, string> result) {
      if (!uri.AbsoluteUri.StartsWith((ResponseUrlOverride ?? ResponseUrl).AbsoluteUri) ||
          uri.Fragment == null) {
        result = null;
        return false;
      }
      string fragmentOrQuery;
      if (!string.IsNullOrEmpty(uri.Fragment)) {
        fragmentOrQuery = uri.Fragment;
      } else {
        fragmentOrQuery = uri.Query;
      }
      // Trim the # or ? off of the fragment/query and then parse the querystring
      result = ParseClient.DecodeQueryString(fragmentOrQuery.Substring(1));
      return true;
    }

    public IDictionary<string, object> GetAuthData(string facebookId, string accessToken, DateTime expiration) {
      return new Dictionary<string, object> {
        {"id", facebookId},
        {"access_token", accessToken},
        {"expiration_date", expiration.ToString(ParseClient.DateFormatStrings.First())}
      };
    }

    public bool HandleNavigation(Uri uri) {
      IDictionary<string, string> result;
      if (TryParseOAuthCallbackUrl(uri, out result)) {
        Action getUserId = () => {
          try {
            if (result.ContainsKey("error")) {
              pendingTask.TrySetException(new ParseException(ParseException.ErrorCode.OtherCause,
                  string.Format("{0}: {1}", result["error_description"], result["error"])));
              return;
            }
            var parameters = new Dictionary<string, object>();
            parameters["access_token"] = result["access_token"];
            parameters["fields"] = "id";

            var request = new HttpRequest {
              Uri = new Uri(MeUrl, "?" + ParseClient.BuildQueryString(parameters)),
              Method = "GET"
            };

            ParseClient.PlatformHooks.HttpClient.ExecuteAsync(request, null, null, CancellationToken.None).OnSuccess(t => {
              var meResult = ParseClient.DeserializeJsonString(t.Result.Item2);
              pendingTask.TrySetResult(GetAuthData(
                  meResult["id"] as string,
                  result["access_token"] as string,
                  (DateTime.Now + TimeSpan.FromSeconds(int.Parse(result["expires_in"])))));
            }).ContinueWith(t => {
              if (t.IsFaulted) {
                pendingTask.TrySetException(t.Exception);
              }
            });
          } catch (Exception e) {
            pendingTask.TrySetException(e);
          }
        };
        getUserId();
        return true;
      }
      return false;
    }

    public Task<IDictionary<string, object>> AuthenticateAsync(CancellationToken cancellationToken) {
      if (AppId == null) {
        throw new InvalidOperationException(
          "You must initialize ParseFacebookUtils before attempting a Facebook login.");
      }
      if (pendingTask != null) {
        pendingTask.TrySetCanceled();
      }
      TaskCompletionSource<IDictionary<string, object>> tcs =
          new TaskCompletionSource<IDictionary<string, object>>();
      pendingCancellationToken = cancellationToken;
      pendingTask = tcs;

      cancellationToken.Register(() => {
        tcs.TrySetCanceled();
      });

      var navigateHandler = Navigate;
      if (navigateHandler != null) {
        var parameters = new Dictionary<string, object>() {
			    {"redirect_uri", (ResponseUrlOverride ?? ResponseUrl).AbsoluteUri},
			    {"response_type", "token"},
			    {"display", "popup"},
			    {"client_id", AppId}
		    };
        if (Permissions != null) {
          parameters["scope"] = string.Join(",", Permissions.ToArray());
        }
        navigateHandler(new Uri(LoginDialogUrlOverride ?? LoginDialogUrl,
            "?" + ParseClient.BuildQueryString(parameters)));
      }
      return tcs.Task;
    }

    public void Deauthenticate() {
      AccessToken = null;
    }

    public bool RestoreAuthentication(IDictionary<string, object> authData) {
      if (authData == null) {
        Deauthenticate();
      } else {
        AccessToken = authData["access_token"] as string;
      }
      return true;
    }

    public string AuthType {
      get { return "facebook"; }
    }
  }
}
