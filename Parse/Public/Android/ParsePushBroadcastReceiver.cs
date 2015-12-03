// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using Parse.Internal;
using Android.App;
using Android.Util;
using Android.Content;
using Android.OS;
using Android.Runtime;

namespace Parse {
  /// <summary>
  /// A <see cref="BroadcastReceiver"/> for rendering and reacting to Notifications.
  /// </summary>
  /// <remarks>
  /// This <see cref="BroadcastReceiver"/> must be registered in order to receive push.
  /// As a security precaution, the intent filters for this <see cref="BroadcastReceiver"/>
  /// must not be exported. Add the following lines to your <c>AndroidManifest.xml fil</c>e, inside the &lt;application&gt;
  /// to properly register the <see cref="ParsePushBroadcastReceiver"/>.
  /// <code>
  /// &lt;receiver android:name="Parse.ParsePushBroadcastReceiver" android:exported=false&gt;
  ///   &lt;intent-filter&gt;
  ///     &lt;action android:name="com.google.android.c2dm.intent.RECEIVE" /&gt;
  ///     &lt;action android:name="com.google.android.c2dm.intent.REGISTRATION" /&gt;
  ///     &lt;category android:name="com.parse.parseunitypushsample" /&gt;
  ///   &lt;/intent-filter&gt;
  /// &lt;/receiver&gt;
  /// </code>
  /// </remarks>
  [Register("parse.ParsePushBroadcastReceiver")]
  public sealed class ParsePushBroadcastReceiver : BroadcastReceiver {
    /// <summary>
    /// The name of the Intent extra which contains the JSON payload of the Notification.
    /// </summary>
    internal const string KeyPushData = "com.parse.Data";

    /// <summary>
    /// The name of the Intent fired when a GCM registration ID is received.
    /// </summary>
    internal const string ActionGcmRegisterResponse = "com.google.android.c2dm.intent.REGISTRATION";

    /// <summary>
    /// The name of the Intent fired when the device received a GCM notification.
    /// </summary>
    internal const string ActionGcmReceive = "com.google.android.c2dm.intent.RECEIVE";

    /// <summary>
    /// Receives push notification and start <see cref="ParsePushService"/> to handle the notification
    /// payload.
    /// </summary>
    /// <remarks>
    /// See <see cref="BroadcastReceiver.OnReceive(Context, Intent)" /> for complete documentation.
    /// </remarks>
    /// <seealso cref="ParsePushService"/>
    /// <param name="context"></param>
    /// <param name="intent"></param>
    public override sealed void OnReceive(Context context, Intent intent) {
      intent.SetClass(context, typeof(ParsePushService));
      ParseWakefulHelper.StartWakefulService(context, intent);
    }
  }
}

