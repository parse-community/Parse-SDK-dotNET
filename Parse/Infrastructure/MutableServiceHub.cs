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
    /// A service hub that is mutable.
    /// </summary>
    /// <remarks>This class is not thread safe; the mutability is allowed for the purposes of overriding values before it is used, as opposed to modifying it while it is in use.</remarks>
    public class MutableServiceHub : IMutableServiceHub
    {
        public IServerConnectionData ServerConnectionData { get; set; }
        public IMetadataController MetadataController { get; set; }

        public IServiceHubCloner Cloner { get; set; }

        public IWebClient WebClient { get; set; }
        public ICacheController CacheController { get; set; }
        public IParseObjectClassController ClassController { get; set; }

        public IParseDataDecoder Decoder { get; set; }

        public IParseInstallationController InstallationController { get; set; }
        public IParseCommandRunner CommandRunner { get; set; }

        public IParseCloudCodeController CloudCodeController { get; set; }
        public IParseConfigurationController ConfigurationController { get; set; }
        public IParseFileController FileController { get; set; }
        public IParseObjectController ObjectController { get; set; }
        public IParseQueryController QueryController { get; set; }
        public IParseSessionController SessionController { get; set; }
        public IParseUserController UserController { get; set; }
        public IParseCurrentUserController CurrentUserController { get; set; }

        public IParseAnalyticsController AnalyticsController { get; set; }

        public IParseInstallationCoder InstallationCoder { get; set; }

        public IParsePushChannelsController PushChannelsController { get; set; }
        public IParsePushController PushController { get; set; }
        public IParseCurrentInstallationController CurrentInstallationController { get; set; }
        public IParseInstallationDataFinalizer InstallationDataFinalizer { get; set; }

        public MutableServiceHub SetDefaults(IServerConnectionData connectionData = default)
        {
            ServerConnectionData ??= connectionData;
            MetadataController ??= new MetadataController
            {
                EnvironmentData = EnvironmentData.Inferred,
                HostManifestData = HostManifestData.Inferred
            };

            Cloner ??= new ConcurrentUserServiceHubCloner { };

            WebClient ??= new UniversalWebClient { };
            CacheController ??= new CacheController { };
            ClassController ??= new ParseObjectClassController { };

            Decoder ??= new ParseDataDecoder(ClassController);

            InstallationController ??= new ParseInstallationController(CacheController);
            CommandRunner ??= new ParseCommandRunner(WebClient, InstallationController, MetadataController, ServerConnectionData, new Lazy<IParseUserController>(() => UserController));

            CloudCodeController ??= new ParseCloudCodeController(CommandRunner, Decoder);
            ConfigurationController ??= new ParseConfigurationController(CommandRunner, CacheController, Decoder);
            FileController ??= new ParseFileController(CommandRunner);
            ObjectController ??= new ParseObjectController(CommandRunner, Decoder, ServerConnectionData);
            QueryController ??= new ParseQueryController(CommandRunner, Decoder);
            SessionController ??= new ParseSessionController(CommandRunner, Decoder);
            UserController ??= new ParseUserController(CommandRunner, Decoder);
            CurrentUserController ??= new ParseCurrentUserController(CacheController, ClassController, Decoder);

            AnalyticsController ??= new ParseAnalyticsController(CommandRunner);

            InstallationCoder ??= new ParseInstallationCoder(Decoder, ClassController);

            PushController ??= new ParsePushController(CommandRunner, CurrentUserController);
            CurrentInstallationController ??= new ParseCurrentInstallationController(InstallationController, CacheController, InstallationCoder, ClassController);
            PushChannelsController ??= new ParsePushChannelsController(CurrentInstallationController);
            InstallationDataFinalizer ??= new ParseInstallationDataFinalizer { };

            return this;
        }
    }
}
