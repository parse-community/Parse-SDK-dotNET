using System;
using System.Linq;
using System.Text;
using Parse.Abstractions.Library;
using Parse.Analytics.Internal;
using Parse.Common.Internal;
using Parse.Core.Internal;
using Parse.Library.Utilities;
using Parse.Push.Internal;

namespace Parse.Library
{

    /// <summary>
    /// A service hub that uses late initialization to efficiently provide controllers and other dependencies to internal Parse SDK systems.
    /// </summary>
    public class ServiceHub : IServiceHub
    {
        LateInitializer LateInitializer { get; } = new LateInitializer { };

        public IServerConnectionData ServerConnectionData { get; set; }
        public IMetadataController MetadataController => LateInitializer.GetValue(() => new MetadataController { HostManifestData = HostManifestData.Inferred, EnvironmentData = EnvironmentData.Inferred });

        public IServiceHubCloner Cloner => LateInitializer.GetValue(() => new { } as object as IServiceHubCloner);

        public IWebClient WebClient => LateInitializer.GetValue(() => new UniversalWebClient { });
        public IStorageController StorageController => LateInitializer.GetValue(() => new StorageController { });
        public IParseObjectClassController ClassController => LateInitializer.GetValue(() => new ParseObjectClassController { });

        public IParseDataDecoder Decoder => LateInitializer.GetValue(() => new ParseDataDecoder(ClassController));

        public IParseInstallationController InstallationController => LateInitializer.GetValue(() => new ParseInstallationController(StorageController));
        public IParseCommandRunner CommandRunner => LateInitializer.GetValue(() => new ParseCommandRunner(WebClient, InstallationController, MetadataController, ServerConnectionData, new Lazy<IParseUserController>(() => UserController)));

        public IParseCloudCodeController CloudCodeController => LateInitializer.GetValue(() => new ParseCloudCodeController(CommandRunner, Decoder));
        public IParseConfigurationController ConfigurationController => LateInitializer.GetValue(() => new ParseConfigurationController(CommandRunner, StorageController, Decoder));
        public IParseFileController FileController => LateInitializer.GetValue(() => new ParseFileController(CommandRunner));
        public IParseObjectController ObjectController => LateInitializer.GetValue(() => new ParseObjectController(CommandRunner, Decoder, ServerConnectionData));
        public IParseQueryController QueryController => LateInitializer.GetValue(() => new ParseQueryController(CommandRunner, Decoder));
        public IParseSessionController SessionController => LateInitializer.GetValue(() => new ParseSessionController(CommandRunner, Decoder));
        public IParseUserController UserController => LateInitializer.GetValue(() => new ParseUserController(CommandRunner, Decoder));
        public IParseCurrentUserController CurrentUserController => LateInitializer.GetValue(() => new ParseCurrentUserController(StorageController, ClassController, Decoder));

        public IParseAnalyticsController AnalyticsController => LateInitializer.GetValue(() => new ParseAnalyticsController(CommandRunner));

        public IParseInstallationCoder InstallationCoder => LateInitializer.GetValue(() => new ParseInstallationCoder(Decoder, ClassController));

        public IParsePushChannelsController PushChannelsController => LateInitializer.GetValue(() => new ParsePushChannelsController(CurrentInstallationController));
        public IParsePushController PushController => LateInitializer.GetValue(() => new ParsePushController(CommandRunner, CurrentUserController));
        public IParseCurrentInstallationController CurrentInstallationController => LateInitializer.GetValue(() => new ParseCurrentInstallationController(InstallationController, StorageController, InstallationCoder, ClassController));
        public IParseInstallationDataFinalizer InstallationDataFinalizer => LateInitializer.GetValue(() => new ParseInstallationDataFinalizer { });

        public bool Reset() => LateInitializer.Used && LateInitializer.Reset();
    }
}
