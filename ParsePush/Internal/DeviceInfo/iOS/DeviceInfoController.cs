using System;
using System.Threading.Tasks;
using LeanCloud.Core.Internal;
using Foundation;

namespace LeanCloud.Push.Internal {
  /// <summary>
  /// This is a concrete implementation of IDeviceInfoController.
  /// Everything is implemented to be a no-op, as an installation
  /// on portable targets can't be used for push notifications.
  /// </summary>
  public class DeviceInfoController : IDeviceInfoController {
    public string DeviceType {
      get { return "ios"; }
    }

    public string DeviceTimeZone {
      get {
        NSTimeZone.ResetSystemTimeZone();
        return NSTimeZone.SystemTimeZone.Name;
      }
    }

    public string AppBuildVersion {
      get { return GetAppAttribute("CFBundleVersion"); }
    }

    public string AppIdentifier {
      get { return GetAppAttribute("CFBundleIdentifier"); }
    }

    public string AppName {
      get { return GetAppAttribute("CFBundleDisplayName"); }
    }

    public Task ExecuteParseInstallationSaveHookAsync(AVInstallation installation) {
      return Task.Run(() => {
        installation.SetIfDifferent("badge", installation.Badge);
      });
    }

    public void Initialize() {
    }

    /// <summary>
    /// Gets an attribute from the Info.plist.
    /// </summary>
    /// <param name="attributeName">the attribute name</param>
    /// <returns>the attribute value</returns>
    /// This is a duplicate of what we have in AVInstallation. We do it because
    /// it's easier to maintain this way (rather than referencing <c>PlatformHooks</c> everywhere).
    private string GetAppAttribute(string attributeName) {
      var appAttributes = NSBundle.MainBundle;

      var attribute = appAttributes.ObjectForInfoDictionary(attributeName);
      return attribute == null ? null : attribute.ToString();
    }
  }
}
