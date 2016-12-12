using System;
using System.Threading.Tasks;
using System.Xml;
using System.Linq;
using LeanCloud.Storage.Internal;
using LeanCloud.Core.Internal;
using System.Collections.Generic;
using Windows.ApplicationModel;
using Windows.Networking.PushNotifications;
using Windows.Storage;
using System.Xml.Linq;

namespace LeanCloud.Push.Internal {
  public class DeviceInfoController : IDeviceInfoController {
    public string DeviceType {
      get {
        return "winrt";
      }
    }

    public string DeviceTimeZone {
      get {
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
      }
    }

    public string AppName {
      get {
        var task = Package.Current.InstalledLocation.GetFileAsync("AppxManifest.xml").AsTask().OnSuccess(t => {
          return FileIO.ReadTextAsync(t.Result).AsTask();
        }).Unwrap().OnSuccess(t => {
          var doc = XDocument.Parse(t.Result);

          // Define the default namespace to be used
          var propertiesXName = XName.Get("Properties", "http://schemas.microsoft.com/appx/2010/manifest");
          var displayNameXName = XName.Get("DisplayName", "http://schemas.microsoft.com/appx/2010/manifest");

          return doc.Descendants(propertiesXName).Single().Descendants(displayNameXName).Single().Value;
        });
        task.Wait();
        return task.Result;
      }
    }

    public string AppBuildVersion {
      get {
        var version = Package.Current.Id.Version;
        return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
      }
    }

    public string AppDisplayVersion {
      get {
        return AppBuildVersion;
      }
    }

    public string AppIdentifier {
      get {
        return Package.Current.Id.Name;
      }
    }

    public Task ExecuteParseInstallationSaveHookAsync(AVInstallation installation) {
      return GetChannelTask.ContinueWith(t => {
        installation.SetIfDifferent("deviceUris", new Dictionary<string, string> {
          { defaultChannelTag, t.Result.Uri }
        });
      });
    }

    public void Initialize() {
    }

    /// <summary>
    /// Future proofing: Right now there's only one valid channel for the app, but we will likely
    /// want to allow additional channels for auxiliary tiles (i.e. a contacts app can have a new
    /// channel for each contact and the UI needs to pop up on the right tile). The expansion job
    /// generically has one _Installation field it passes to device-specific code, so we store a map
    /// of tag -> channel URI. Right now, there is only one valid tag and it is automatic.
    /// Unused variable warnings are suppressed because this const is used in WinRT and WinPhone but not NetFx.
    /// </summary>
    private static readonly string defaultChannelTag = "_Default";

    // This must be wrapped in a property so other classes may continue on this task
    // during their static initialization.
    private static Lazy<Task<PushNotificationChannel>> getChannelTask = new Lazy<Task<PushNotificationChannel>>(() =>
      PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync().AsTask()
    );
    internal static Task<PushNotificationChannel> GetChannelTask {
      get {
        return getChannelTask.Value;
      }
    }

    static DeviceInfoController() {
      var _ = GetChannelTask;
    }
  }
}