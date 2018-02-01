using System;
using Parse.Core.Internal;

namespace Parse.Push.Internal
{
    public interface IParsePushPlugins
    {
        void Reset();

        IParseCorePlugins CorePlugins { get; }
        IParsePushChannelsController PushChannelsController { get; }
        IParsePushController PushController { get; }
        IParseCurrentInstallationController CurrentInstallationController { get; }
        IDeviceInfoController DeviceInfoController { get; }
    }
}