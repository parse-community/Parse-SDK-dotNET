using System;
using System.Threading.Tasks;

namespace Parse.Push.Internal
{
    public interface IDeviceInfoController
    {
        string DeviceType { get; }
        string DeviceTimeZone { get; }
        string AppBuildVersion { get; }
        string AppIdentifier { get; }
        string AppName { get; }


        /// <summary>
        /// Executes platform specific hook that mutate the installation based on
        /// the device platforms.
        /// </summary>
        /// <param name="installation">Installation to be mutated.</param>
        /// <returns></returns>
        Task ExecuteParseInstallationSaveHookAsync(ParseInstallation installation);

        void Initialize();
    }
}
