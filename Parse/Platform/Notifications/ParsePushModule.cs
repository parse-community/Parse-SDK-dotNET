using Parse.Common.Internal;
using Parse.Management;

namespace Parse.Push.Internal
{
    public class ParsePushModule : IParseModule
    {
        public void OnModuleRegistered()
        {
        }

        public void OnParseInitialized()
        {
            ParseObject.RegisterDerivative<ParseInstallation>();

            ParseCorePlugins.Instance.SubclassingController.AddRegisterHook(typeof(ParseInstallation), () =>
            {
                ParsePushPlugins.Instance.CurrentInstallationController.ClearFromMemory();
            });

            ParsePushPlugins.Instance.DeviceInfoController.Initialize();
        }
    }
}