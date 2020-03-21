using Parse.Core.Internal;

namespace Parse.Push.Internal
{
    public class ParsePushPlugins : IParsePushPlugins
    {
        private static readonly object instanceMutex = new object();
        private static IParsePushPlugins instance;
        public static IParsePushPlugins Instance
        {
            get
            {
                instance = instance ?? new ParsePushPlugins();
                return instance;
            }
            set
            {
                lock (instanceMutex)
                {
                    instance = value;
                }
            }
        }

        private readonly object mutex = new object();

        private IParseCorePlugins corePlugins;
        private IParsePushChannelsController pushChannelsController;
        private IParsePushController pushController;
        private IParseCurrentInstallationController currentInstallationController;
        private IDeviceInfoController deviceInfoController;

        public void Reset()
        {
            lock (mutex)
            {
                CorePlugins = null;
                PushChannelsController = null;
                PushController = null;
                CurrentInstallationController = null;
                DeviceInfoController = null;
            }
        }

        public IParseCorePlugins CorePlugins
        {
            get
            {
                lock (mutex)
                {
                    corePlugins = corePlugins ?? ParseCorePlugins.Instance;
                    return corePlugins;
                }
            }
            set
            {
                lock (mutex)
                {
                    corePlugins = value;
                }
            }
        }

        public IParsePushChannelsController PushChannelsController
        {
            get
            {
                lock (mutex)
                {
                    pushChannelsController = pushChannelsController ?? new ParsePushChannelsController();
                    return pushChannelsController;
                }
            }
            set
            {
                lock (mutex)
                {
                    pushChannelsController = value;
                }
            }
        }

        public IParsePushController PushController
        {
            get
            {
                lock (mutex)
                {
                    pushController = pushController ?? new ParsePushController(CorePlugins.CommandRunner, CorePlugins.CurrentUserController);
                    return pushController;
                }
            }
            set
            {
                lock (mutex)
                {
                    pushController = value;
                }
            }
        }

        public IParseCurrentInstallationController CurrentInstallationController
        {
            get
            {
                lock (mutex)
                {
                    currentInstallationController = currentInstallationController ?? new ParseCurrentInstallationController(
                      CorePlugins.InstallationIdController, CorePlugins.StorageController, ParseInstallationCoder.Instance
                    );
                    return currentInstallationController;
                }
            }
            set
            {
                lock (mutex)
                {
                    currentInstallationController = value;
                }
            }
        }

        public IDeviceInfoController DeviceInfoController
        {
            get
            {
                lock (mutex)
                {
                    deviceInfoController = deviceInfoController ?? new DeviceInfoController();
                    return deviceInfoController;
                }
            }
            set
            {
                lock (mutex)
                {
                    deviceInfoController = value;
                }
            }
        }
    }
}
