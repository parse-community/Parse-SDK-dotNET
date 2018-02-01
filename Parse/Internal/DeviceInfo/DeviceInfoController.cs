using System;
using System.Threading.Tasks;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.DotNet.PlatformAbstractions;
using ApplicationEnvironmentEx = Microsoft.Extensions.PlatformAbstractions.ApplicationEnvironment;
using ApplicationEnvironment = Microsoft.DotNet.PlatformAbstractions.ApplicationEnvironment;
using System.Runtime.Versioning;

namespace Parse.Push.Internal
{

    /// <summary>
    /// This is a concrete implementation of IDeviceInfoController.
    /// Everything is implemented to be a no-op, as an installation
    /// on portable targets can't be used for push notifications.
    /// </summary>
    public class DeviceInfoController : IDeviceInfoController
    {
        static ApplicationEnvironmentEx ApplicationEnvironment { get; } = new ApplicationEnvironmentEx { };
        public string DeviceType
        {
            get
            { return RuntimeEnvironment.OperatingSystem.ToString(); }
        }

        public string DeviceTimeZone
        {
            get { return TimeZoneInfo.Local.StandardName; }
        }

        public string AppBuildVersion
        {
            get { return ApplicationEnvironment.ApplicationVersion; }
        }

        public string AppIdentifier
        {
            get { return AppDomain.CurrentDomain.FriendlyName; }
        }

        public string AppName
        {
            get { return ApplicationEnvironment.ApplicationName; }
        }

        public Task ExecuteParseInstallationSaveHookAsync(ParseInstallation installation)
        {
            return Task.FromResult<object>(null);
        }

        public void Initialize()
        {
        }
    }
}