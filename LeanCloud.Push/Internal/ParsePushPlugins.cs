using LeanCloud.Core.Internal;
using System;

namespace LeanCloud.Push.Internal {
  public class AVPushPlugins : IAVPushPlugins {
    private static readonly object instanceMutex = new object();
    private static IAVPushPlugins instance;
    public static IAVPushPlugins Instance {
      get {
        instance = instance ?? new AVPushPlugins();
        return instance;
      }
      set {
        lock (instanceMutex) {
          instance = value;
        }
      }
    }

    private readonly object mutex = new object();

    private IAVCorePlugins corePlugins;
    private IAVPushChannelsController pushChannelsController;
    private IAVPushController pushController;
    private IAVCurrentInstallationController currentInstallationController;
    private IDeviceInfoController deviceInfoController;

    public void Reset() {
      lock (mutex) {
        CorePlugins = null;
        PushChannelsController = null;
        PushController = null;
        CurrentInstallationController = null;
        DeviceInfoController = null;
      }
    }

    public IAVCorePlugins CorePlugins {
      get {
        lock (mutex) {
          corePlugins = corePlugins ?? AVPlugins.Instance;
          return corePlugins;
        }
      }
      set {
        lock (mutex) {
          corePlugins = value;
        }
      }
    }

    public IAVPushChannelsController PushChannelsController {
      get {
        lock (mutex) {
          pushChannelsController = pushChannelsController ?? new AVPushChannelsController();
          return pushChannelsController;
        }
      }
      set {
        lock (mutex) {
          pushChannelsController = value;
        }
      }
    }

    public IAVPushController PushController {
      get {
        lock (mutex) {
          pushController = pushController ?? new AVPushController(CorePlugins.CommandRunner, CorePlugins.CurrentUserController);
          return pushController;
        }
      }
      set {
        lock (mutex) {
          pushController = value;
        }
      }
    }

    public IAVCurrentInstallationController CurrentInstallationController {
      get {
        lock (mutex) {
          currentInstallationController = currentInstallationController ?? new AVCurrentInstallationController(
            CorePlugins.InstallationIdController, CorePlugins.StorageController, AVInstallationCoder.Instance
          );
          return currentInstallationController;
        }
      }
      set {
        lock (mutex) {
          currentInstallationController = value;
        }
      }
    }

    public IDeviceInfoController DeviceInfoController {
      get {
        lock (mutex) {
          deviceInfoController = deviceInfoController ?? new DeviceInfoController();
          return deviceInfoController;
        }
      }
      set {
        lock (mutex) {
          deviceInfoController = value;
        }
      }
    }
  }
}
