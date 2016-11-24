using System;
using LeanCloud.Core.Internal;

namespace LeanCloud.Push.Internal {
  public interface IParsePushPlugins {
    void Reset();

    IAVCorePlugins CorePlugins { get; }
    IParsePushChannelsController PushChannelsController { get; }
    IParsePushController PushController { get; }
    IParseCurrentInstallationController CurrentInstallationController { get; }
    IDeviceInfoController DeviceInfoController { get; }
  }
}