using Parse.Abstractions.Management;

namespace Parse.Push.Internal
{
    public interface IParsePushPlugins
    {
        void Reset();

        IParseCorePlugins CorePlugins { get; }
        IParsePushChannelsController PushChannelsController { get; }
        IParsePushController PushController { get; }
        IParseCurrentInstallationController CurrentInstallationController { get; }
        IParseInstallationDataFinalizer DeviceInfoController { get; }
    }
}