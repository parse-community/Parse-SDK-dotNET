using Parse.Common.Internal;
using Parse.Core.Internal;
using System;

namespace Parse.Push.Internal
{
    public class ParsePushModule : IParseModule
    {
        public void OnModuleRegistered()
        {
        }

        public void OnParseInitialized()
        {
            ParseObject.RegisterSubclass<ParseInstallation>();

            ParseCorePlugins.Instance.SubclassingController.AddRegisterHook(typeof(ParseInstallation), () =>
            {
                ParsePushPlugins.Instance.CurrentInstallationController.ClearFromMemory();
            });

            ParsePushPlugins.Instance.DeviceInfoController.Initialize();
        }
    }
}