using System;
using System.Threading.Tasks;
using Microsoft.Extensions.PlatformAbstractions;

namespace Parse.Push.Internal
{

    /// <summary>
    /// Controls the device information.
    /// </summary>
    public class DeviceInfoController : IDeviceInfoController
    {
        static ApplicationEnvironment ApplicationEnvironment { get; } = new ApplicationEnvironment { };

        /// <summary>
        /// The device platform that the app is currently running on.
        /// </summary>
        public string DeviceType => Environment.OSVersion.ToString();

        /// <summary>
        /// The active time zone on the device that the app is currently running on.
        /// </summary>
        public string DeviceTimeZone => TimeZoneInfo.Local.StandardName;

        /// <summary>
        /// The version number of the application.
        /// </summary>
        public string AppBuildVersion => ApplicationEnvironment.ApplicationVersion;

        /// <summary>
        /// The identifier of the application
        /// </summary>
        public string AppIdentifier => AppDomain.CurrentDomain.FriendlyName;

        /// <summary>
        /// The name of the current application.
        /// </summary>
        public string AppName => ApplicationEnvironment.ApplicationName;

        public Task ExecuteParseInstallationSaveHookAsync(ParseInstallation installation) => Task.FromResult<object>(null);

        public void Initialize() { }
    }
}