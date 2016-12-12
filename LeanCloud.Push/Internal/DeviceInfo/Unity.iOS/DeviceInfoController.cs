using System;
using System.Threading.Tasks;
using LeanCloud.Core.Internal;
using LeanCloud.Storage.Internal;
using System.Collections.Generic;
using NotificationServices = UnityEngine.iOS.NotificationServices;
using UnityEngine;

namespace LeanCloud.Push.Internal {
  /// <summary>
  /// This is a concrete implementation of IDeviceInfoController for Unity iOS targets.
  /// </summary>
  public class DeviceInfoController : IDeviceInfoController {
    public string DeviceType {
      get { return "ios"; }
    }

    public string DeviceTimeZone {
      get {
        try {
          // We need the system string to be in english so we'll have the proper key in our lookup table.
          // If it's not in english then we will attempt to fallback to the closest Time Zone we can find.
          TimeZoneInfo tzInfo = TimeZoneInfo.Local;

          string deviceTimeZone = null;
          if (AVInstallation.TimeZoneNameMap.TryGetValue(tzInfo.StandardName, out deviceTimeZone)) {
            return deviceTimeZone;
          }

          TimeSpan utcOffset = tzInfo.BaseUtcOffset;

          // If we have an offset that is not a round hour, then use our second map to see if we can
          // convert it or not.
          if (AVInstallation.TimeZoneOffsetMap.TryGetValue(utcOffset, out deviceTimeZone)) {
            return deviceTimeZone;
          }

          // NOTE: Etc/GMT{+/-} format is inverted from the UTC offset we use as normal people -
          // a negative value means ahead of UTC, a positive value means behind UTC.
          bool negativeOffset = utcOffset.Ticks < 0;
          return String.Format("Etc/GMT{0}{1}", negativeOffset ? "+" : "-", Math.Abs(utcOffset.Hours));
        } catch (TimeZoneNotFoundException) {
          return null;
        }
      }
    }

    private string appBuildVersion;
    public string AppBuildVersion {
      get { return appBuildVersion; }
    }

    public string AppIdentifier {
      get {
        ApplicationIdentity identity = AppDomain.CurrentDomain.ApplicationIdentity;
        if (identity == null) {
          return null;
        }
        return identity.FullName;
      }
    }

    private string appName;
    public string AppName {
      get { return appName; }
    }

    public Task ExecuteParseInstallationSaveHookAsync(AVInstallation installation) {
      return Task.Run(() => {
        installation.SetIfDifferent("badge", installation.Badge);
      });
    }

    public void Initialize() {
      // We can only set some values here since we can be sure that Initialize is always called
      // from main thread.
      appBuildVersion = Application.version;
      appName = Application.productName;

      RegisterDeviceTokenRequest(deviceToken => {
        if (deviceToken == null) {
          return;
        }

        AVInstallation installation = AVInstallation.CurrentInstallation;
        installation.SetDeviceTokenFromData(deviceToken);

        // Optimistically assume this will finish.
        installation.SaveAsync();
      });
    }

    /// <summary>
    /// Registers a callback for a device token request.
    /// </summary>
    /// <param name="action"></param>
    private void RegisterDeviceTokenRequest(Action<byte[]> action) {
      Dispatcher.Instance.Post(() => {
        var deviceToken = NotificationServices.deviceToken;
        if (deviceToken == null) {
          RegisterDeviceTokenRequest(action);
          return;
        }

        action(deviceToken);
        RegisteriOSPushNotificationListener((payload) => {
          AVPush.parsePushNotificationReceived.Invoke(AVInstallation.CurrentInstallation, new AVPushNotificationEventArgs(payload));
        });
      });
    }

    /// <summary>
    /// Registers a callback for push notifications.
    /// </summary>
    /// <param name="action"></param>
    private void RegisteriOSPushNotificationListener(Action<IDictionary<string, object>> action) {
      Dispatcher.Instance.Post(() => {
        // Check in every frame
        RegisteriOSPushNotificationListener(action);

        int remoteNotificationCount = NotificationServices.remoteNotificationCount;
        if (remoteNotificationCount == 0) {
          return;
        }

        var remoteNotifications = NotificationServices.remoteNotifications;
        foreach (var val in remoteNotifications) {
          var userInfo = val.userInfo;
          var payload = new Dictionary<string, object>();
          foreach (var key in userInfo.Keys) {
            payload[key.ToString()] = userInfo[key];
          }

          // Finally, invoke the action for the remote notification payload.
          action(payload);
        }

        NotificationServices.ClearRemoteNotifications();
      });
    }
  }
}