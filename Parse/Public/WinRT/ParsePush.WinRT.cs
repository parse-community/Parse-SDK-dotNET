// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using Parse.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Networking.PushNotifications;
using Windows.UI.Notifications;

namespace Parse {
  public partial class ParsePush {

    static ParsePush() {
      PlatformHooks.GetChannelTask.ContinueWith(t =>
        t.Result.PushNotificationReceived += (sender, args) => {
          PushNotificationReceived(ParseInstallation.CurrentInstallation, args);

          var payload = PushJson(args);
          var handler = parsePushNotificationReceived;
          if (handler != null) {
            handler.Invoke(ParseInstallation.CurrentInstallation, new ParsePushNotificationEventArgs(payload));
          }
        }
      );
    }

    /// <summary>
    /// An event fired when a push notification of any type (i.e. toast, tile, badge, or raw) is
    /// received.
    /// </summary>
    public static event EventHandler<PushNotificationReceivedEventArgs> PushNotificationReceived;

    /// <summary>
    /// An event fired when a toast notification is received.
    /// </summary>
    public static event EventHandler<PushNotificationReceivedEventArgs> ToastNotificationReceived {
      add {
          PushNotificationReceived += value;
      }
      remove {
          PushNotificationReceived -= value;
      }
    }

    /// <summary>
    /// Helper method to extract the full Push JSON provided to Parse, including any
    /// non-visual custom information. Overloads exist for all data types which may be
    /// provided by Windows, I.E. LaunchActivatedEventArgs and PushNotificationReceivedEventArgs.
    /// Returns an empty dictionary if this push type cannot include non-visual data or
    /// this event was not triggered by a push.
    /// </summary>
    public static IDictionary<string, object> PushJson(ILaunchActivatedEventArgs eventArgs) {
      if (eventArgs == null ||
          eventArgs.Kind != ActivationKind.Launch ||
          eventArgs.Arguments == null) {
        return new Dictionary<string, object>();
      }
      return PushJson(eventArgs.Arguments);
    }

    /// <summary>
    /// Helper method to extract the full Push JSON provided to Parse, including any
    /// non-visual custom information. Overloads exist for all data types which may be
    /// provided by Windows, I.E. LaunchActivatedEventArgs and PushNotificationReceivedEventArgs.
    /// Returns an empty dictionary if this push type cannot include non-visual data or
    /// this event was not triggered by a push.
    /// </summary>
    public static IDictionary<string, object> PushJson(PushNotificationReceivedEventArgs eventArgs) {
      var toast = eventArgs.ToastNotification;
      if (toast == null) {
        return new Dictionary<string, object>();
      }
      return PushJson(toast);
    }

    /// <summary>
    /// Because the Windows API doesn't allow us to create a PushNotificationReceivedEventArgs, nor is there
    /// an interface for the class, we cannot test the PushJson(PNREA) method at all. We will instead try to
    /// make it as small as possible and test a method that uses the first class which does allow instantiation.
    /// </summary>
    /// <param name="toast"></param>
    /// <returns></returns>
    internal static IDictionary<string, object> PushJson(ToastNotification toast) {
      var xml = toast.Content;
      var node = xml.GetElementsByTagName("toast")[0].Attributes.GetNamedItem("launch");
      if (node == null) {
        return new Dictionary<string, object>();
      }
      return PushJson((string)node.NodeValue);
    }

    private static IDictionary<string, object> PushJson(string jsonString) {
      try {
        return ParseClient.DeserializeJsonString(jsonString) ?? new Dictionary<string, object>();
      } catch (Exception) {
        return new Dictionary<string, object>();
      }
    }
  }


}
