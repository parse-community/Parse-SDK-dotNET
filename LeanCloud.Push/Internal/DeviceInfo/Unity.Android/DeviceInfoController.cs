using System;
using System.Threading.Tasks;
using LeanCloud.Core.Internal;
using UnityEngine;
using LeanCloud.Storage.Internal;

namespace LeanCloud.Push.Internal
{
    /// <summary>
    /// This is a concrete implementation of IDeviceInfoController for Unity Android targets.
    /// </summary>
    public class DeviceInfoController : IDeviceInfoController
    {
        /// <summary>
        /// Helper class that can listen to android-specific unity messages.
        /// </summary>
        private class GCMRegistrationCallbackBehavior : MonoBehaviour
        {
            /// <summary>
            /// Delegate function that will be called when the player pauses the game.
            /// </summary>
            /// <seealso href="http://docs.unity3d.com/ScriptReference/MonoBehaviour.OnApplicationPause.html"/>
            /// <param name="paused"><c>true</c> if the application is paused.</param>
            public void OnApplicationPause(bool paused)
            {
                AndroidJavaClass javaUnityHelper = new AndroidJavaClass("com.parse.ParsePushUnityHelper");
                javaUnityHelper.CallStatic("setApplicationPaused", new object[] { paused });
            }

            /// <summary>
            /// The callback that will be called from the Android Java land via <c>UnityPlayer.UnitySendMessage(string)</c>
            /// when the device receive a push notification.
            /// </summary>
            /// <param name="pushPayloadString">the push payload as string</param>
            internal void OnPushNotificationReceived(string pushPayloadString)
            {
                AVInitializeBehaviour behavior = gameObject.GetComponent<AVInitializeBehaviour>();
                if (behavior != null)
                {
                    behavior.Initialize();
                }

                AVPush.parsePushNotificationReceived.Invoke(AVInstallation.CurrentInstallation, new AVPushNotificationEventArgs(pushPayloadString));
            }

            /// <summary>
            /// The callback that will be called from the Android Java land via <c>UnityPlayer.UnitySendMessage(string)</c>
            /// when the device receive a GCM registration id.
            /// </summary>
            /// <param name="registrationId">the GCM registration id</param>
            internal void OnGcmRegistrationReceived(string registrationId)
            {
                //Initialize();

                var installation = AVInstallation.CurrentInstallation;
                installation.DeviceToken = registrationId;
                // Set `pushType` via internal `Set` method since we want to skip mutability check.
                installation.Set("pushType", "gcm");

                // We can't really wait for this or else we'll block the thread.
                // We can only hope this operation will finish.
                installation.SaveAsync();
            }
        }

        public string DeviceType
        {
            get { return "android"; }
        }

        public string DeviceTimeZone
        {
            get
            {
                try
                {
                    // We need the system string to be in english so we'll have the proper key in our lookup table.
                    // If it's not in english then we will attempt to fallback to the closest Time Zone we can find.
                    TimeZoneInfo tzInfo = TimeZoneInfo.Local;

                    string deviceTimeZone = null;
                    if (AVInstallation.TimeZoneNameMap.TryGetValue(tzInfo.StandardName, out deviceTimeZone))
                    {
                        return deviceTimeZone;
                    }

                    TimeSpan utcOffset = tzInfo.BaseUtcOffset;

                    // If we have an offset that is not a round hour, then use our second map to see if we can
                    // convert it or not.
                    if (AVInstallation.TimeZoneOffsetMap.TryGetValue(utcOffset, out deviceTimeZone))
                    {
                        return deviceTimeZone;
                    }

                    // NOTE: Etc/GMT{+/-} format is inverted from the UTC offset we use as normal people -
                    // a negative value means ahead of UTC, a positive value means behind UTC.
                    bool negativeOffset = utcOffset.Ticks < 0;
                    return String.Format("Etc/GMT{0}{1}", negativeOffset ? "+" : "-", Math.Abs(utcOffset.Hours));
                }
                catch (TimeZoneNotFoundException)
                {
                    return null;
                }
            }
        }

        private string appBuildVersion;
        public string AppBuildVersion
        {
            get { return appBuildVersion; }
        }

        public string AppIdentifier
        {
            get
            {
                ApplicationIdentity identity = AppDomain.CurrentDomain.ApplicationIdentity;
                if (identity == null)
                {
                    return null;
                }
                return identity.FullName;
            }
        }

        private string appName;
        public string AppName
        {
            get { return appName; }
        }

        public Task ExecuteParseInstallationSaveHookAsync(AVInstallation installation)
        {
            return Task.Run(() =>
            {
                installation.SetIfDifferent("badge", installation.Badge);
            });
        }

        public void Initialize()
        {
            // We can only set some values here since we can be sure that Initialize is always called
            // from main thread.
            appBuildVersion = Application.version;
            appName = Application.productName;

            // Add our GCM callback listener
            Dispatcher.Instance.GameObject.AddComponent<GCMRegistrationCallbackBehavior>();

            try
            {
                AndroidJavaClass javaUnityHelper = new AndroidJavaClass("com.parse.ParsePushUnityHelper");
                javaUnityHelper.CallStatic("registerGcm", null);
            }
            catch (Exception e)
            {
                // We don't care about the exception. If it reaches this point, it means the Plugin is misconfigured/we don't want to use
                // PushNotification. Let's just log it to developer.
                Debug.LogException(e);
            }
        }
    }
}