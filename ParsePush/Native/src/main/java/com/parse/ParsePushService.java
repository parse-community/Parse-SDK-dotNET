/*
 * Copyright (c) 2015-present, Parse, LLC.
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree. An additional grant
 * of patent rights can be found in the PATENTS file in the same directory.
 */
package com.parse;

import android.app.IntentService;
import android.content.Context;
import android.content.Intent;
import android.os.Bundle;
import android.os.IBinder;
import android.os.PowerManager;
import android.util.Log;

import com.google.android.gms.gcm.GoogleCloudMessaging;
import com.unity3d.player.UnityPlayer;

import java.io.IOException;

/**
 * A {@link IntentService} to handle push notification payload in background job.
 */
public class ParsePushService extends IntentService {
  private static final String TAG = "ParsePushService";

  private static final String PARSE_GCM_SENDER_ID = "1076345567071";

  /** The name of the Intent fired to register for GCM. */
  /* package */ static final String ACTION_PUSH_REGISTER = "com.parse.push.intent.REGISTER";

  /** The name of the Intent fired when the device received a GCM notification. */
  /* package */ static final String ACTION_GCM_RECEIVE = "com.google.android.c2dm.intent.RECEIVE";

  /** The key of <meta-data> Bundle which contains the developer GCM sender ID. */
  private static final String EXTRA_SENDER_ID = "com.parse.push.gcm_sender_id";

  private static PowerManager.WakeLock wakeLock;
  private static final Object WAKE_LOCK_MUTEX = new Object();

  public ParsePushService() {
    super(TAG);
  }

  @Override
  protected void onHandleIntent(Intent intent) {
    try {
      switch (intent.getAction()) {
        case ACTION_GCM_RECEIVE:
          onPushNotificationReceived(intent.getStringExtra("data"));
          break;
        case ACTION_PUSH_REGISTER:
          GoogleCloudMessaging gcm = GoogleCloudMessaging.getInstance(this);
          try {
            // TODO (hallucinogen): try to get it from SharedPreferences
            String registrationId = gcm.register(getGcmSenderId());
            onGcmRegistrationReceived(registrationId);
          } catch (IOException ex) {
            // Do nothing.
          }
          break;
        default:
          // Do nothing.
          break;
      }
    } finally {
      synchronized (WAKE_LOCK_MUTEX) {
        if (wakeLock != null) {
          wakeLock.release();
        }
      }
    }
  }

  private String getGcmSenderId() {
    ManifestInfo info = new ManifestInfo(this);
    Bundle metadata = info.getApplicationMetadata();
    String senderIds = PARSE_GCM_SENDER_ID;
    if (metadata == null) {
      return senderIds;
    }

    Object senderIdExtra = metadata.get(EXTRA_SENDER_ID);
    if (senderIdExtra == null || !(senderIdExtra instanceof String)) {
      return senderIds;
    }

    String senderId = (String)senderIdExtra;
    if (!senderId.startsWith("id:")) {
      Log.e(TAG, "Found " + EXTRA_SENDER_ID + " <meta-data> element with value \"" +
          senderIdExtra.toString() + "\", but the value is missing the expected \"id:\" " +
          "prefix.");
      return senderIds;
    }
    senderIds += "," + senderId.substring(3);

    return senderIds;
  }

  /**
   * Wakes up device and starts {@link ParsePushService}
   * @param context Context to initiate the service.
   * @param intent Intent passed into the service.
   */
  /* package */ static void startWakefulIntentService(Context context, Intent intent) {
    synchronized (WAKE_LOCK_MUTEX) {
      if (wakeLock == null) {
        PowerManager powerManager = (PowerManager)context.getSystemService(POWER_SERVICE);
        wakeLock = powerManager.newWakeLock(PowerManager.PARTIAL_WAKE_LOCK, TAG);
      }
    }

    wakeLock.acquire();
    intent.setClass(context, ParsePushService.class);
    context.startService(intent);
  }

  /**
   * Client code should not call {@code onBind} directly.
   */
  @Override
  public IBinder onBind(Intent intent) {
    throw new IllegalArgumentException("You cannot bind directly to the ParsePushService.");
  }

  // region Handling GCM

  // TODO (hallucinogen): t6967779. These two methods should be abstract and
  // overridden by UnityParsePushService that calls UnityPlayer.UnitySendMessage.
  // We should also remove classes.jar from ParsePush since native Android doesn't need it.
  protected void onPushNotificationReceived(String pushPayloadString) {
    Log.i(TAG, "Push notification received. Payload: " + pushPayloadString);
    try {
      if (!ParsePushUnityHelper.isApplicationPaused() && pushPayloadString != null) {
        UnityPlayer.UnitySendMessage("ParseInitializeBehaviour",
            "OnPushNotificationReceived",
            pushPayloadString);
        Log.i(TAG, "Push notification is handled while the app is foregrounded.");
        return;
      }
    } catch (UnsatisfiedLinkError error) {
      // This means Unity hasn't started yet. Do nothing.
    }
    // This means the app is in background (whether it's killed or backgrounded).
    // While we try to wait for Unity to start, let's fire our default behavior.
    ParsePushUnityHelper.handleParsePushNotificationReceived(this, pushPayloadString);
    Log.i(TAG, "Push notification is handled while the app is backgrounded.");
  }

  protected void onGcmRegistrationReceived(String registrationId) {
    Log.i(TAG, "GCM registration successful. Registration Id: " + registrationId);
    try {
      if (!ParsePushUnityHelper.isApplicationPaused()) {
        UnityPlayer.UnitySendMessage("ParseInitializeBehaviour",
            "OnGcmRegistrationReceived",
            registrationId);
        Log.i(TAG, "GCM registration is handled while the app is foregrounded.");
        return;
      }
    } catch (UnsatisfiedLinkError error) {
      // This means Unity hasn't started yet. Do nothing.
    }
    // This means the app is in background (whether it's killed or backgrounded).
    Log.e(TAG, "Cannot save Installation because GCM registration is handled while the app is" +
        "backgrounded.");
  }

  // endregion
}
