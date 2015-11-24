// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using Parse.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Android.Content;
using Android.App;

namespace Parse {
  public partial class ParsePush {
    /// <summary>
    /// Attach this method to <see cref="ParsePush.ParsePushNotificationReceived"/> to utilize a
    /// default handler for push notification.
    /// </summary>
    /// <remarks>
    /// This handler will try to get the launcher <see cref="Activity"/> and application icon, then construct a
    /// <see cref="Notification"/> out of them. It uses push payload's <c>title</c> and <c>alert</c> as the
    /// <see cref="Notification.ContentView"/> title and text.
    /// </remarks>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public static void DefaultParsePushNotificationReceivedHandler(object sender, ParsePushNotificationEventArgs args) {
      IDictionary<string, object> pushData = args.Payload;
      Context context = Application.Context;

      if (pushData == null || (!pushData.ContainsKey("alert") && !pushData.ContainsKey("title"))) {
        return;
      }

      string title = pushData.ContainsKey("title") ? pushData["title"] as string : ManifestInfo.DisplayName;
      string alert = pushData.ContainsKey("alert") ? pushData["alert"] as string : "Notification received.";
      string tickerText = title + ": " + alert;

      Random random = new Random();
      int contentIntentRequestCode = random.Next();

      Intent activityIntent = ManifestInfo.LauncherIntent;
      PendingIntent pContentIntent = PendingIntent.GetActivity(context, contentIntentRequestCode, activityIntent, PendingIntentFlags.UpdateCurrent);

      NotificationCompat.Builder builder = new NotificationCompat.Builder(context)
        .SetContentTitle(new Java.Lang.String(title))
        .SetContentText(new Java.Lang.String(alert))
        .SetTicker(new Java.Lang.String(tickerText))
        .SetSmallIcon(ManifestInfo.PushIconId)
        .SetContentIntent(pContentIntent)
        .SetAutoCancel(true)
        .SetDefaults(NotificationDefaults.All);

      Notification notification = builder.Build();
      NotificationManager manager = context.GetSystemService(Context.NotificationService) as NotificationManager;
      int notificationId = (int)DateTime.UtcNow.Ticks;

      try {
        manager.Notify(notificationId, notification);
      } catch (Exception) {
        // Some phones throw exception for unapproved vibration.
        notification.Defaults = NotificationDefaults.Lights | NotificationDefaults.Sound;
        manager.Notify(notificationId, notification);
      }
    }

    /// <summary>
    /// Helper method to extract the full Push JSON provided to Parse, including any
    /// non-visual custom information.
    /// </summary>
    /// <param name="intent"><see cref="Android.Content.Intent"/> received typically
    /// from a <see cref="Android.Content.BroadcastReceiver"/></param>
    /// <returns>Returns the push payload in the intent. Returns an empty dictionary if the intent
    /// doesn't contain push payload.</returns>
    internal static IDictionary<string, object> PushJson(Intent intent) {
      IDictionary<string, object> result = new Dictionary<string, object>();
      string messageType = intent.GetStringExtra("message_type");

      if (messageType != null) {
         // The GCM docs reserve the right to use the message_type field for new actions, but haven't
         // documented what those new actions are yet. For forwards compatibility, ignore anything
         // with a message_type field.
      } else {
        string pushDataString = intent.GetStringExtra("data");
        IDictionary<string, object> pushData = null;

        // We encode the data payload as JSON string. Deserialize that string now.
        if (pushDataString != null) {
          pushData = ParseClient.DeserializeJsonString(pushDataString);
        }

        if (pushData != null) {
          result = pushData;
        }
      }

      return result;
    }
  }
}
