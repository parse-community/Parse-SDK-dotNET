using System;
using System.Threading.Tasks;
using Microsoft.Extensions.PlatformAbstractions;

namespace Parse.Push.Internal
{

    /// <summary>
    /// This is a concrete implementation of IDeviceInfoController.
    /// Everything is implemented to be a no-op, as an installation
    /// on portable targets can't be used for push notifications.
    /// </summary>
    public class DeviceInfoController : IDeviceInfoController
    {
        static ApplicationEnvironment ApplicationEnvironment { get; } = new ApplicationEnvironment { };

        public string DeviceType => Environment.OSVersion.ToString();

        public string DeviceTimeZone => TimeZoneInfo.Local.StandardName;

        public string AppBuildVersion => ApplicationEnvironment.ApplicationVersion;

        public string AppIdentifier => AppDomain.CurrentDomain.FriendlyName;

        public string AppName => ApplicationEnvironment.ApplicationName;

        public Task ExecuteParseInstallationSaveHookAsync(ParseInstallation installation) => Task.FromResult<object>(null);

        public void Initialize() { }
    }
}