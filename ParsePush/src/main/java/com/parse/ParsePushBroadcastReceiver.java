/*
 * Copyright (c) 2015-present, Parse, LLC.
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree. An additional grant
 * of patent rights can be found in the PATENTS file in the same directory.
 */
package com.parse;

import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;

/**
 * A {@link BroadcastReceiver} for rendering and reacting to to Notifications.
 * <p/>
 * This {@link BroadcastReceiver} must be registered in order to use the Parse Push.
 * As a security precaution, the intent filters for this {@link BroadcastReceiver} must not be
 * exported. Add the following lines to your {@code AndroidManifest.xml} file, inside the
 * &lt;application&gt; element to properly register the {@code ParsePushBroadcastReceiver}:
 * <p/>
 * <pre>
 * &lt;receiver android:name="com.parse.ParsePushBroadcastReceiver" android:exported=false&gt;
 *  &lt;intent-filter&gt;
 *     &lt;action android:name="com.google.android.c2dm.intent.RECEIVE" /&gt;
 *     &lt;action android:name="com.google.android.c2dm.intent.REGISTRATION" /&gt;
 *     &lt;category android:name="YOUR_PACKAGE_NAME" /&gt;
 *   &lt;/intent-filter&gt;
 * &lt;/receiver&gt;
 * </pre>
 * <p/>
 */
public class ParsePushBroadcastReceiver extends BroadcastReceiver {
  @Override
  public void onReceive(Context context, Intent intent) {
    ParsePushService.startWakefulIntentService(context, intent);
  }
}
