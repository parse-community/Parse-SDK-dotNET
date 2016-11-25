using LeanCloud.Common.Internal;
using LeanCloud.Core.Internal;
using System;

namespace LeanCloud.Push.Internal {
  public class ParsePushModule : IAVModule {
    public void OnModuleRegistered() {
    }

    public void OnParseInitialized() {
      AVObject.RegisterSubclass<AVInstallation>();

      AVPlugins.Instance.SubclassingController.AddRegisterHook(typeof(AVInstallation), () => {
        ParsePushPlugins.Instance.CurrentInstallationController.ClearFromMemory();
      });

      ParsePushPlugins.Instance.DeviceInfoController.Initialize();
    }
  }
}