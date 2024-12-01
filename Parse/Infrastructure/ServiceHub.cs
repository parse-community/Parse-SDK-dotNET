using System;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Platform.Analytics;
using Parse.Abstractions.Platform.Cloud;
using Parse.Abstractions.Platform.Configuration;
using Parse.Abstractions.Platform.Files;
using Parse.Abstractions.Platform.Installations;
using Parse.Abstractions.Platform.Objects;
using Parse.Abstractions.Platform.Push;
using Parse.Abstractions.Platform.Queries;
using Parse.Abstractions.Platform.Sessions;
using Parse.Abstractions.Platform.Users;
using Parse.Infrastructure.Data;
using Parse.Infrastructure.Execution;
using Parse.Infrastructure.Utilities;
using Parse.Platform.Analytics;
using Parse.Platform.Cloud;
using Parse.Platform.Configuration;
using Parse.Platform.Files;
using Parse.Platform.Installations;
using Parse.Platform.Objects;
using Parse.Platform.Push;
using Parse.Platform.Queries;
using Parse.Platform.Sessions;
using Parse.Platform.Users;

namespace Parse.Infrastructure
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
        public ICacheController CacheController => LateInitializer.GetValue(() => new CacheController { });
        public IParseObjectClassController ClassController => LateInitializer.GetValue(() => new ParseObjectClassController { });

        public IParseDataDecoder Decoder => LateInitializer.GetValue(() => new ParseDataDecoder(ClassController));

        public IParseInstallationController InstallationController => LateInitializer.GetValue(() => new ParseInstallationController(CacheController));
        public IParseCommandRunner CommandRunner => LateInitializer.GetValue(() => new ParseCommandRunner(WebClient, InstallationController, MetadataController, ServerConnectionData, new Lazy<IParseUserController>(() => UserController)));

        public IParseCloudCodeController CloudCodeController => LateInitializer.GetValue(() => new ParseCloudCodeController(CommandRunner, Decoder));
        public IParseConfigurationController ConfigurationController => LateInitializer.GetValue(() => new ParseConfigurationController(CommandRunner, CacheController, Decoder));
        public IParseFileController FileController => LateInitializer.GetValue(() => new ParseFileController(CommandRunner));
        public IParseObjectController ObjectController => LateInitializer.GetValue(() => new ParseObjectController(CommandRunner, Decoder, ServerConnectionData));
        public IParseQueryController QueryController => LateInitializer.GetValue(() => new ParseQueryController(CommandRunner, Decoder));
        public IParseSessionController SessionController => LateInitializer.GetValue(() => new ParseSessionController(CommandRunner, Decoder));
        public IParseUserController UserController => LateInitializer.GetValue(() => new ParseUserController(CommandRunner, Decoder));
        public IParseCurrentUserController CurrentUserController => LateInitializer.GetValue(() => new ParseCurrentUserController(CacheController, ClassController, Decoder));

        public IParseAnalyticsController AnalyticsController => LateInitializer.GetValue(() => new ParseAnalyticsController(CommandRunner));

        public IParseInstallationCoder InstallationCoder => LateInitializer.GetValue(() => new ParseInstallationCoder(Decoder, ClassController));

        public IParsePushChannelsController PushChannelsController => LateInitializer.GetValue(() => new ParsePushChannelsController(CurrentInstallationController));
        public IParsePushController PushController => LateInitializer.GetValue(() => new ParsePushController(CommandRunner, CurrentUserController));
        public IParseCurrentInstallationController CurrentInstallationController => LateInitializer.GetValue(() => new ParseCurrentInstallationController(InstallationController, CacheController, InstallationCoder, ClassController));
        public IParseInstallationDataFinalizer InstallationDataFinalizer => LateInitializer.GetValue(() => new ParseInstallationDataFinalizer { });

        public bool Reset()
        {
            return LateInitializer.Used && LateInitializer.Reset();
        }
    }
}
