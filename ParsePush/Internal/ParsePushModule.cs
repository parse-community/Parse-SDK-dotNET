using LeanCloud.Common.Internal;
using LeanCloud.Core.Internal;
using System;

namespace LeanCloud.Push.Internal {
  public class ParsePushModule : IAVModule {
    public void OnModuleRegistered() {
    }

    public void OnParseInitialized() {
      AVObject.RegisterSubclass<ParseInstallation>();

      AVPlugins.Instance.SubclassingController.AddRegisterHook(typeof(ParseInstallation), () => {
        ParsePushPlugins.Instance.CurrentInstallationController.ClearFromMemory();
      });

      ParsePushPlugins.Instance.DeviceInfoController.Initialize();
    }
  }
}