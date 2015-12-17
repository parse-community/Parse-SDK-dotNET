// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Parse {
  /// <summary>
  /// Mandatory MonoBehaviour for scenes that use Parse. Set the application ID and .NET key
  /// in the editor.
  /// </summary>
  // TODO (hallucinogen): somehow because of Push, we need this class to be added in a GameObject
  // called `ParseInitializeBehaviour`. We might want to fix this.
  public class ParseInitializeBehaviour : MonoBehaviour {
    private static bool isInitialized = false;

    /// <summary>
    /// The Parse applicationId used in this app. You can get this value from the Parse website.
    /// </summary>
    [SerializeField]
    public string applicationID;

    /// <summary>
    /// The Parse dotnetKey used in this app. You can get this value from the Parse website.
    /// </summary>
    [SerializeField]
    public string dotnetKey;

    /// <summary>
    /// Initializes the Parse SDK and begins running network requests created by Parse.
    /// </summary>
    public virtual void Awake() {
      Initialize();
      // Force the name to be `ParseInitializeBehaviour` in runtime.
      gameObject.name = "ParseInitializeBehaviour";

      if (PlatformHooks.IsIOS) {
        PlatformHooks.RegisterDeviceTokenRequest((deviceToken) => {
          if (deviceToken != null) {
            ParseInstallation installation = ParseInstallation.CurrentInstallation;
            installation.SetDeviceTokenFromData(deviceToken);

            // Optimistically assume this will finish.
            installation.SaveAsync();
          }
        });
      }
    }

    /// <summary>
    /// Delegate function that will be called when the player pauses the game.
    /// </summary>
    /// <seealso href="http://docs.unity3d.com/ScriptReference/MonoBehaviour.OnApplicationPause.html"/>
    /// <param name="paused"><c>true</c> if the application is paused.</param>
    public void OnApplicationPause(bool paused) {
      if (PlatformHooks.IsAndroid) {
        PlatformHooks.CallStaticJavaUnityMethod("com.parse.ParsePushUnityHelper", "setApplicationPaused", new object[] { paused });
      }
    }

    private void Initialize() {
      if (!isInitialized) {
        isInitialized = true;
        // Keep this gameObject around, even when the scene changes.
        GameObject.DontDestroyOnLoad(gameObject);

        ParseClient.Initialize(applicationID, dotnetKey);

        // Kick off the dispatcher.
        StartCoroutine(PlatformHooks.RunDispatcher());
      }
    }

    #region Android Callbacks

    /// <summary>
    /// The callback that will be called from the Android Java land via <c>UnityPlayer.UnitySendMessage(string)</c>
    /// when the device receive a push notification.
    /// </summary>
    /// <param name="pushPayloadString">the push payload as string</param>
    internal void OnPushNotificationReceived(string pushPayloadString) {
      Initialize();

      ParsePush.parsePushNotificationReceived.Invoke(ParseInstallation.CurrentInstallation, new ParsePushNotificationEventArgs(pushPayloadString));
    }

    /// <summary>
    /// The callback that will be called from the Android Java land via <c>UnityPlayer.UnitySendMessage(string)</c>
    /// when the device receive a GCM registration id.
    /// </summary>
    /// <param name="registrationId">the GCM registration id</param>
    internal void OnGcmRegistrationReceived(string registrationId) {
      Initialize();

      var installation = ParseInstallation.CurrentInstallation;
      installation.DeviceToken = registrationId;
      // Set `pushType` via internal `Set` method since we want to skip mutability check.
      installation.Set("pushType", "gcm");

      // We can't really wait for this or else we'll block the thread.
      // We can only hope this operation will finish.
      installation.SaveAsync();
    }

    #endregion
  }
}
