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
      get { return null; }
    }

    public string DeviceTimeZone {
      get { return null; }
    }

    public string AppBuildVersion {
      get { return null; }
    }

    public string AppIdentifier {
      get { return null; }
    }

    public string AppName {
      get { return null; }
    }

    public Task ExecuteParseInstallationSaveHookAsync(AVInstallation installation) {
      return Task.FromResult<object>(null);
    }

    public void Initialize() {
    }
  }
}