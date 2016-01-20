// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Parse.Internal.Analytics.Controller;

namespace Parse {
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
        var json = ParsePush.PushJson(args.Uri.ToString());
		object alert = null;
		if(json.TryGetValue("alert", out alert) || alwaysReport) {  
		  string pushHash = ParseAnalyticsUtilities.MD5DigestFromPushPayload(alert);
          await TrackAppOpenedWithPushHashAsync(pushHash);
        }
      };
    }
  }
}
