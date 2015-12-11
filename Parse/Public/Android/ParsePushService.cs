// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Util;
using Android.OS;
using Android.Runtime;
using Parse.Internal;

namespace Parse {
  /// <summary>
  /// An <see cref="IntentService"/> to handle push notification payload in background job.
  /// </summary>
  [Register("parse.ParsePushService")]
  public sealed class ParsePushService : IntentService {
    private const int IntentServiceHandlerTimeout = 10000;

    /// <summary>
    /// Handle push intent from <see cref="ParsePushBroadcastReceiver"/>.
    /// </summary>
    /// <param name="intent">The intent to be handled.</param>
    protected override void OnHandleIntent(Intent intent) {
      Task task = Task.FromResult(0);
      try {
        // Assume only GCM intent is received here.
        switch (intent.Action) {
          case ParsePushBroadcastReceiver.ActionGcmRegisterResponse:
            task = GcmRegistrar.GetInstance().HandleRegistrationIntentAsync(intent);
            break;
          case ParsePushBroadcastReceiver.ActionGcmReceive:
            if (ManifestInfo.HasPermissionForGCM()) {
              ParsePush.parsePushNotificationReceived.Invoke(ParseInstallation.CurrentInstallation, new ParsePushNotificationEventArgs(ParsePush.PushJson(intent)));
            }
            break;
          default:
            // TODO (hallucinogen): Prints error that we don't support other intent.
            break;
        }
        // Wait for its completion with timeout.
        task.Wait(IntentServiceHandlerTimeout);
      } finally {
        ParseWakefulHelper.CompleteWakefulIntent(intent);
      }
    }
  }
}

