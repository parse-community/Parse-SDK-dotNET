using System;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Platform.Installations;
using Parse.Abstractions.Platform.Objects;
using Parse.Abstractions.Platform.Cloud;
using Parse.Abstractions.Platform.Configuration;
using Parse.Abstractions.Platform.Files;
using Parse.Abstractions.Platform.Push;
using Parse.Abstractions.Platform.Queries;
using Parse.Abstractions.Platform.Sessions;
using Parse.Abstractions.Platform.Users;
using Parse.Abstractions.Platform.Analytics;
using Parse.Infrastructure.Execution;
using Parse.Platform.Objects;
using Parse.Platform.Installations;
using Parse.Platform.Cloud;
using Parse.Platform.Configuration;
using Parse.Platform.Files;
using Parse.Platform.Queries;
using Parse.Platform.Sessions;
using Parse.Platform.Users;
using Parse.Platform.Analytics;
using Parse.Platform.Push;
using Parse.Infrastructure.Data;
using Parse.Infrastructure.Utilities;

namespace Parse.Infrastructure
{
    public class LateInitializedMutableServiceHub : IMutableServiceHub
    {
        LateInitializer LateInitializer { get; } = new LateInitializer { };

        public IServiceHubCloner Cloner { get; set; }

        public IMetadataController MetadataController
        {
            get => LateInitializer.GetValue<IMetadataController>(() => new MetadataController { EnvironmentData = EnvironmentData.Inferred, HostManifestData = HostManifestData.Inferred });
            set => LateInitializer.SetValue(value);
        }

        public IWebClient WebClient
        {
            get => LateInitializer.GetValue<IWebClient>(() => new UniversalWebClient { });
            set => LateInitializer.SetValue(value);
        }

        public ICacheController CacheController
        {
            get => LateInitializer.GetValue<ICacheController>(() => new CacheController { });
            set => LateInitializer.SetValue(value);
        }

        public IParseObjectClassController ClassController
        {
            get => LateInitializer.GetValue<IParseObjectClassController>(() => new ParseObjectClassController { });
            set => LateInitializer.SetValue(value);
        }

        public IParseInstallationController InstallationController
        {
            get => LateInitializer.GetValue<IParseInstallationController>(() => new ParseInstallationController(CacheController));
            set => LateInitializer.SetValue(value);
        }

        public IParseCommandRunner CommandRunner
        {
            get => LateInitializer.GetValue<IParseCommandRunner>(() => new ParseCommandRunner(WebClient, InstallationController, MetadataController, ServerConnectionData, new Lazy<IParseUserController>(() => UserController)));
            set => LateInitializer.SetValue(value);
        }

        public IParseCloudCodeController CloudCodeController
        {
            get => LateInitializer.GetValue<IParseCloudCodeController>(() => new ParseCloudCodeController(CommandRunner, Decoder));
            set => LateInitializer.SetValue(value);
        }

        public IParseConfigurationController ConfigurationController
        {
            get => LateInitializer.GetValue<IParseConfigurationController>(() => new ParseConfigurationController(CommandRunner, CacheController, Decoder));
            set => LateInitializer.SetValue(value);
        }

        public IParseFileController FileController
        {
            get => LateInitializer.GetValue<IParseFileController>(() => new ParseFileController(CommandRunner));
            set => LateInitializer.SetValue(value);
        }

        public IParseObjectController ObjectController
        {
            get => LateInitializer.GetValue<IParseObjectController>(() => new ParseObjectController(CommandRunner, Decoder, ServerConnectionData));
            set => LateInitializer.SetValue(value);
        }

        public IParseQueryController QueryController
        {
            get => LateInitializer.GetValue<IParseQueryController>(() => new ParseQueryController(CommandRunner, Decoder));
            set => LateInitializer.SetValue(value);
        }

        public IParseSessionController SessionController
        {
            get => LateInitializer.GetValue<IParseSessionController>(() => new ParseSessionController(CommandRunner, Decoder));
            set => LateInitializer.SetValue(value);
        }

        public IParseUserController UserController
        {
            get => LateInitializer.GetValue<IParseUserController>(() => new ParseUserController(CommandRunner, Decoder));
            set => LateInitializer.SetValue(value);
        }

        public IParseCurrentUserController CurrentUserController
        {
            get => LateInitializer.GetValue(() => new ParseCurrentUserController(CacheController, ClassController, Decoder));
            set => LateInitializer.SetValue(value);
        }

        public IParseAnalyticsController AnalyticsController
        {
            get => LateInitializer.GetValue<IParseAnalyticsController>(() => new ParseAnalyticsController(CommandRunner));
            set => LateInitializer.SetValue(value);
        }

        public IParseInstallationCoder InstallationCoder
        {
            get => LateInitializer.GetValue<IParseInstallationCoder>(() => new ParseInstallationCoder(Decoder, ClassController));
            set => LateInitializer.SetValue(value);
        }

        public IParsePushChannelsController PushChannelsController
        {
            get => LateInitializer.GetValue<IParsePushChannelsController>(() => new ParsePushChannelsController(CurrentInstallationController));
            set => LateInitializer.SetValue(value);
        }

        public IParsePushController PushController
        {
            get => LateInitializer.GetValue<IParsePushController>(() => new ParsePushController(CommandRunner, CurrentUserController));
            set => LateInitializer.SetValue(value);
        }

        public IParseCurrentInstallationController CurrentInstallationController
        {
            get => LateInitializer.GetValue<IParseCurrentInstallationController>(() => new ParseCurrentInstallationController(InstallationController, CacheController, InstallationCoder, ClassController));
            set => LateInitializer.SetValue(value);
        }

        public IParseDataDecoder Decoder
        {
            get => LateInitializer.GetValue<IParseDataDecoder>(() => new ParseDataDecoder(ClassController));
            set => LateInitializer.SetValue(value);
        }

        public IParseInstallationDataFinalizer InstallationDataFinalizer
        {
            get => LateInitializer.GetValue<IParseInstallationDataFinalizer>(() => new ParseInstallationDataFinalizer { });
            set => LateInitializer.SetValue(value);
        }

        public IServerConnectionData ServerConnectionData { get; set; }
    }
}
