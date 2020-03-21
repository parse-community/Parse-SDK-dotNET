using Parse.Common.Internal;
using Parse.Core.Internal;

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