using LeanCloud.Storage.Internal;
using LeanCloud.Core.Internal;
using System;

namespace LeanCloud.Push.Internal {
  public class AVPushModule : IAVModule {
    public void OnModuleRegistered() {
    }

    public void OnParseInitialized() {
      AVObject.RegisterSubclass<AVInstallation>();

      AVPlugins.Instance.SubclassingController.AddRegisterHook(typeof(AVInstallation), () => {
        AVPushPlugins.Instance.CurrentInstallationController.ClearFromMemory();
      });

      AVPushPlugins.Instance.DeviceInfoController.Initialize();
    }
  }
}