using System;
using System.Threading.Tasks;

namespace LeanCloud.Push.Internal {
  /// <summary>
  /// This is a concrete implementation of IDeviceInfoController.
  /// Everything is implemented to be a no-op, as an installation
  /// on portable targets can't be used for push notifications.
  /// </summary>
  public class DeviceInfoController : IDeviceInfoController {
    public string DeviceType {
      get {
        return "android";
      }
    }

    public string DeviceTimeZone {
      get {
        return Java.Util.TimeZone.Default.ID;
      }
    }

    public string AppBuildVersion {
      get {
        return ManifestInfo.VersionCode.ToString();
      }
    }

    public string AppIdentifier {
      get {
        return ManifestInfo.PackageName;
      }
    }

    public string AppName {
      get {
        return ManifestInfo.DisplayName;
      }
    }

    public Task ExecuteParseInstallationSaveHookAsync(AVInstallation installation) {
      return Task.FromResult<object>(null);
    }

    public void Initialize() {
      if (ManifestInfo.HasPermissionForGCM()) {
        GcmRegistrar.GetInstance().Register();
      }
    }
  }
}
