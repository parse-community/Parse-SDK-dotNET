using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Parse;
using System;

public class PushBehaviour : MonoBehaviour {
  // Use this for initialization
  void Awake() {
#if UNITY_IOS
    NotificationServices.RegisterForRemoteNotificationTypes(RemoteNotificationType.Alert |
                                                            RemoteNotificationType.Badge |
                                                            RemoteNotificationType.Sound);
#endif

    ParsePush.ParsePushNotificationReceived += (sender, args) => {
#if UNITY_ANDROID
      AndroidJavaClass parseUnityHelper = new AndroidJavaClass("com.parse.ParsePushUnityHelper");
      AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
      AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

      // Call default behavior.
      parseUnityHelper.CallStatic("handleParsePushNotificationReceived", currentActivity, args.StringPayload);
#elif UNITY_IOS
      IDictionary<string, object> payload = args.Payload;

      foreach (var key in payload.Keys) {
        Debug.Log("Payload: " + key + ": " + payload[key]);
      }
#endif
    };
  }
}
