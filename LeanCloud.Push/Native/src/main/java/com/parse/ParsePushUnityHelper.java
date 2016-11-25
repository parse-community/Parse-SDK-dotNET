/*
 * Copyright (c) 2015-present, Parse, LLC.
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree. An additional grant
 * of patent rights can be found in the PATENTS file in the same directory.
 */
package com.parse;

import android.app.Notification;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.Context;
import android.content.Intent;
import android.support.v4.app.NotificationCompat;
import android.util.Log;

import com.unity3d.player.UnityPlayer;

import org.json.JSONException;
import org.json.JSONObject;

import java.util.Objects;
import java.util.Random;

/**
 * A helper class that exposes basic native Android interface to be invoked from the Unity land.
 */
/** package */ class ParsePushUnityHelper {
  private static final String TAG = "ParsePushUnityHelper";

  private static final Object MUTEX = new Object();
  private static boolean applicationPaused = false;

  /**
   * A helper method to identify the Unity game paused state. We're using this approach since we
   * don't want to force developers to extend UnityPlayerActivity.onPause.
   *
   * @param applicationPaused
   */
  protected static void setApplicationPaused(boolean applicationPaused) {
    synchronized (MUTEX) {
      ParsePushUnityHelper.applicationPaused = applicationPaused;
    }
  }

  /** package */ static boolean isApplicationPaused() {
    synchronized (MUTEX) {
      return applicationPaused;
    }
  }

  /**
   * A helper method that provides default behavior for handling ParsePushNotificationReceived.
   */
  public static void handleParsePushNotificationReceived(Context context,
      String pushPayloadString) {
    try {
      JSONObject pushData = new JSONObject(pushPayloadString);

      if (pushData == null || (!pushData.has("alert") && !pushData.has("title"))) {
        return;
      }

      ManifestInfo info = new ManifestInfo(context);
      String title = pushData.optString("title", info.getDisplayName());
      String alert = pushData.optString("alert", "Notification received.");
      String tickerText = title + ": " + alert;

      Random random = new Random();
      int contentIntentRequestCode = random.nextInt();

      Intent activityIntent = info.getLauncherIntent();
      PendingIntent pContentIntent = PendingIntent.getActivity(
          context, contentIntentRequestCode, activityIntent, PendingIntent.FLAG_UPDATE_CURRENT);

      NotificationCompat.Builder builder = new NotificationCompat.Builder(context)
          .setContentTitle(title)
          .setContentText(alert)
          .setTicker(tickerText)
          .setSmallIcon(info.getPushIconId())
          .setContentIntent(pContentIntent)
          .setAutoCancel(true)
          .setDefaults(Notification.DEFAULT_ALL);

      Notification notification = builder.build();
      NotificationManager manager =
          (NotificationManager)context.getSystemService(Context.NOTIFICATION_SERVICE);
      int notificationId = (int)System.currentTimeMillis();

      try {
        manager.notify(notificationId, notification);
      } catch (SecurityException se) {
        // Some phones throw exception for unapproved vibration.
        notification.defaults = Notification.DEFAULT_LIGHTS | Notification.DEFAULT_SOUND;
        manager.notify(notificationId, notification);
      }
    } catch (JSONException e) {
      // Do nothing.
    }
  }

  /**
   * A helper method to create GCM registrationId. Will only be called through reflection.
   *
   * Remarks: We need a dummy object here because when using reflection from C#
   * to call Java bridge, you can't use `null` to stub optional param.
   * Adding `null` will make the bridge consider that you're actually
   * calling `registerGcm(java.lang.Object)`. Without the null, the bridge won't
   * find the method (since the Java bridge signature is
   * `CallStatic(string, params object[])`.
   */
  protected static void registerGcm(Object notUsed) {
    Context context = UnityPlayer.currentActivity.getApplicationContext();
    // Only register if we have permission.
    ManifestInfo info = new ManifestInfo(context);

    if (info.hasPermissionForGcm()) {
      Intent intent = new Intent(ParsePushService.ACTION_PUSH_REGISTER);
      Log.i(TAG, "ParsePushService started for GCM registration.");
      ParsePushService.startWakefulIntentService(context, intent);
    }
  }
}
