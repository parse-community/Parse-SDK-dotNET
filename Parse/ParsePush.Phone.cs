// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using Microsoft.Phone.Notification;
using Parse.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace Parse {
  public partial class ParsePush {
    static ParsePush() {
      PlatformHooks.GetToastChannelTask.ContinueWith(t => {
        if (t.Result != null) {
          t.Result.ShellToastNotificationReceived += (sender, args) => {
            toastNotificationReceived.Invoke(ParseInstallation.CurrentInstallation, args);
            var payload = PushJson(args);
            parsePushNotificationReceived.Invoke(ParseInstallation.CurrentInstallation, new ParsePushNotificationEventArgs(payload));
          };
          t.Result.HttpNotificationReceived += (sender, args) => {
            pushNotificationReceived.Invoke(ParseInstallation.CurrentInstallation, args);

            // TODO (hallucinogen): revisit this since we haven't officially support this yet.
            var payloadStream = args.Notification.Body;
            var streamReader = new StreamReader(payloadStream);
            var payloadString = streamReader.ReadToEnd();

            // Always assume it's a JSON payload.
            var payload = ParseClient.DeserializeJsonString(payloadString);
            parsePushNotificationReceived.Invoke(ParseInstallation.CurrentInstallation, new ParsePushNotificationEventArgs(payload));
          };
        }
      });
    }

    /// <summary>
    /// An event fired when a push notification of any type (i.e. toast, tile, badge, or raw) is
    /// received.
    /// </summary>
    public static event EventHandler<NotificationEventArgs> ToastNotificationReceived {
      add {
        toastNotificationReceived.Add(value);
      }
      remove {
        toastNotificationReceived.Remove(value);
      }
    }

    /// <summary>
    /// A generic event handler for notifications of all types. Because this event is also fired
    /// when a raw notification is sent, the event args are very hard to use. You only get a byte
    /// stream! We'll reveal this publicly once we support raw notifications; in the meantime we
    /// should leave ToastNotificationReceived as the golden road.
    /// </summary>
    internal static event EventHandler<HttpNotificationEventArgs> PushNotificationReceived {
      add {
        pushNotificationReceived.Add(value);
      }
      remove {
        pushNotificationReceived.Remove(value);
      }
    }

    /// <summary>
    /// Extract the JSON dictionary used to send this push.
    /// </summary>
    /// <param name="args">The args parameter passed to a push received event.</param>
    /// <returns>The JSON dictionary used to send this push.</returns>
    public static IDictionary<string, object> PushJson(NotificationEventArgs args) {
      string launchString = null;
      if (!args.Collection.TryGetValue("wp:Param", out launchString)) {
        return new Dictionary<string, object>();
      }
      return PushJson(launchString);
    }

    /// <summary>
    /// A method for getting the JSON dictionary used to send a push notification from the
    /// OnNavigated event handler, i.e.
    /// 
    /// <code>
    /// public override void OnNavigatedTo(NavigationEventArgs args) {
    ///   var json = PushJson(args);
    ///   /* ... */
    /// }
    /// </code>
    /// </summary>
    /// <param name="args">The args parameter passed to OnNavigatedTo</param>
    /// <returns>The JSON dictionary used to send this push.</returns>
    public static IDictionary<string, object> PushJson(NavigationEventArgs args) {
      return PushJson(args.Uri.ToString());
    }

    internal static IDictionary<string, object> PushJson(string uri) {
      var queryTokens = uri.Substring(uri.LastIndexOf('?') + 1).Split('&');
      foreach (var token in queryTokens) {
        if (token.StartsWith("pushJson=")) {
          var rawValue = token.Substring("pushJson=".Length);
          var decoded = HttpUtility.UrlDecode(rawValue);
          return ParseClient.DeserializeJsonString(decoded);
        }
      }
      return new Dictionary<string, object>();
    }

    private static readonly SynchronizedEventHandler<NotificationEventArgs> toastNotificationReceived = new SynchronizedEventHandler<NotificationEventArgs>();
    private static readonly SynchronizedEventHandler<HttpNotificationEventArgs> pushNotificationReceived = new SynchronizedEventHandler<HttpNotificationEventArgs>();
  }
}
