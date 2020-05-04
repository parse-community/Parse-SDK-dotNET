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

namespace Parse.Infrastructure
{
    public class OrchestrationServiceHub : IServiceHub
    {
        public IServiceHub Default { get; set; }

        public IServiceHub Custom { get; set; }

        public IServiceHubCloner Cloner => Custom.Cloner ?? Default.Cloner;

        public IMetadataController MetadataController => Custom.MetadataController ?? Default.MetadataController;

        public IWebClient WebClient => Custom.WebClient ?? Default.WebClient;

        public ICacheController CacheController => Custom.CacheController ?? Default.CacheController;

        public IParseObjectClassController ClassController => Custom.ClassController ?? Default.ClassController;

        public IParseInstallationController InstallationController => Custom.InstallationController ?? Default.InstallationController;

        public IParseCommandRunner CommandRunner => Custom.CommandRunner ?? Default.CommandRunner;

        public IParseCloudCodeController CloudCodeController => Custom.CloudCodeController ?? Default.CloudCodeController;

        public IParseConfigurationController ConfigurationController => Custom.ConfigurationController ?? Default.ConfigurationController;

        public IParseFileController FileController => Custom.FileController ?? Default.FileController;

        public IParseObjectController ObjectController => Custom.ObjectController ?? Default.ObjectController;

        public IParseQueryController QueryController => Custom.QueryController ?? Default.QueryController;

        public IParseSessionController SessionController => Custom.SessionController ?? Default.SessionController;

        public IParseUserController UserController => Custom.UserController ?? Default.UserController;

        public IParseCurrentUserController CurrentUserController => Custom.CurrentUserController ?? Default.CurrentUserController;

        public IParseAnalyticsController AnalyticsController => Custom.AnalyticsController ?? Default.AnalyticsController;

        public IParseInstallationCoder InstallationCoder => Custom.InstallationCoder ?? Default.InstallationCoder;

        public IParsePushChannelsController PushChannelsController => Custom.PushChannelsController ?? Default.PushChannelsController;

        public IParsePushController PushController => Custom.PushController ?? Default.PushController;

        public IParseCurrentInstallationController CurrentInstallationController => Custom.CurrentInstallationController ?? Default.CurrentInstallationController;

        public IServerConnectionData ServerConnectionData => Custom.ServerConnectionData ?? Default.ServerConnectionData;

        public IParseDataDecoder Decoder => Custom.Decoder ?? Default.Decoder;

        public IParseInstallationDataFinalizer InstallationDataFinalizer => Custom.InstallationDataFinalizer ?? Default.InstallationDataFinalizer;
    }
}
