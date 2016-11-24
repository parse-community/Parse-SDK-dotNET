// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using Microsoft.Phone.Controls;
using LeanCloud.Common.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LeanCloud {
  public partial class ParseAnalytics {
    /// <summary>
    /// This method adds event listeners to track app opens from tiles, the app list,
    /// and push notifications. Windows Phone 8 developers should use TrackAppOpens instead of
    /// TrackAppOpenedAsync, which this method will call automatically.
    ///
    /// This method can be called in Application_Launching or as follows in the Application constructor:
    ///
    /// <code>
    /// this.Startup += (sender, args) => {
    ///   ParseAnalytics.TrackAppOpens(RootFrame);
    /// };
    /// </code>
    /// </summary>
    /// <param name="frame">The RootFrame of the Application.</param>
    public static void TrackAppOpens(PhoneApplicationFrame frame) {
      // This method is supposed to be called from OnLaunched. This call may also be
      // an app open; if it doesn't contain a valid push hash, make sure that it's counted once upon startup.
      var isFirstLaunch = true;

      frame.Navigated += async (sender, args) => {
        bool alwaysReport = isFirstLaunch;
        isFirstLaunch = false;

        // If the user navigates to a push, goes to a new activity, and then goes back,
        // we shouldn't double count the push.
        if (args.NavigationMode != System.Windows.Navigation.NavigationMode.New) {
          return;
        }
        var json = PushJson(args.Uri.ToString());
        object hash = null;
        if (json.TryGetValue("push_hash", out hash) || alwaysReport) {
          await TrackAppOpenedWithPushHashAsync((string)hash);
        }
      };
    }


    /// <summary>
    /// Extract the JSON dictionary used to send this push.
    /// </summary>
    /// <param name="uri">The args parameter passed to a push received event.</param>
    private static IDictionary<string, object> PushJson(string uri) {
      var queryTokens = uri.Substring(uri.LastIndexOf('?') + 1).Split('&');
      foreach (var token in queryTokens) {
        if (token.StartsWith("pushJson=")) {
          var rawValue = token.Substring("pushJson=".Length);
          var decoded = HttpUtility.UrlDecode(rawValue);
          return Json.Parse(decoded) as IDictionary<string, object>;
        }
      }
      return new Dictionary<string, object>();
    }
  }
}
