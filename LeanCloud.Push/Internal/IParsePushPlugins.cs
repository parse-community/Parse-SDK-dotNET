using System;
using LeanCloud.Core.Internal;

namespace LeanCloud.Push.Internal {
  public interface IAVPushPlugins {
    void Reset();

    IAVCorePlugins CorePlugins { get; }
    IAVPushChannelsController PushChannelsController { get; }
    IAVPushController PushController { get; }
    IAVCurrentInstallationController CurrentInstallationController { get; }
    IDeviceInfoController DeviceInfoController { get; }
  }
}