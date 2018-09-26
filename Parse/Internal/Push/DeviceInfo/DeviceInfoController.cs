using System;
using System.Threading.Tasks;

namespace Parse.Push.Internal
{

    /// <summary>
    /// Controls the device information.
    /// </summary>
    public class DeviceInfoController : IDeviceInfoController
    {
        /// <summary>
        /// The device platform that the app is currently running on.
        /// </summary>
        public string DeviceType { get; } = Environment.OSVersion.ToString();

        /// <summary>
        /// The active time zone on the device that the app is currently running on.
        /// </summary>
        public string DeviceTimeZone => TimeZoneInfo.Local.StandardName;

        /// <summary>
        /// The version number of the application.
        /// </summary>
        public string AppBuildVersion { get; } = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.Build.ToString();

        // TODO: Verify if this means Parse appId or just a unique identifier.

        /// <summary>
        /// The identifier of the application
        /// </summary>
        public string AppIdentifier => AppDomain.CurrentDomain.FriendlyName;

        /// <summary>
        /// The name of the current application.
        /// </summary>
        public string AppName { get; } = System.Reflection.Assembly.GetEntryAssembly().GetName().Name;

        public Task ExecuteParseInstallationSaveHookAsync(ParseInstallation installation) => Task.FromResult<object>(null);

        public void Initialize() { }
    }
}