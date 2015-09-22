// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Foundation;
using UIKit;
using Parse.Internal;

namespace Parse {
  public partial class ParsePush {
    /// <summary>
    /// HandlePush is a push notification handler that allows you to listen to push notifications using
    /// <see cref="ParsePushNotificationReceived"/>.
    /// </summary>
    /// <remarks>
    /// Call this from <c>AppDelegate</c>'s <c>ReceiveRemoteNotification</c>.
    /// </remarks>
    /// <param name="userInfo">The <c>userInfo</c> dictionary you get in <c>ReceiveRemoteNotification</c>.</param>
    public static void HandlePush(NSDictionary userInfo) {
      UIApplication application = UIApplication.SharedApplication;
      var payload = PushJson(userInfo);

      parsePushNotificationReceived.Invoke(ParseInstallation.CurrentInstallation, new ParsePushNotificationEventArgs(payload));
    }

    /// <summary>
    /// Helper method to extract the full Push JSON provided to Parse, including any
    /// non-visual custom information.
    /// </summary>
    /// <param name="userInfo">userInfo dictionary from <see cref="UIApplicationDelegate.ReceivedRemoteNotification"/></param>
    /// <returns>Returns the push payload in <c>userInfo</c> as dictionary. Returns an empty dictionary if <c>userInfo</c>
    /// doesn't contain push payload.</returns>
    public static IDictionary<string, object> PushJson(NSDictionary userInfo) {
      IDictionary<string, object> result = new Dictionary<string, object>();

      foreach (var keyValuePair in userInfo) {
        result.Add(new KeyValuePair<string, object>(keyValuePair.Key.ToString(), keyValuePair.Value));
      }

      return result;
    }
  }
}