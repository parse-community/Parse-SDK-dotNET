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

namespace Parse.Abstractions.Infrastructure
{
    public abstract class CustomServiceHub : ICustomServiceHub
    {
        public virtual IServiceHub Services { get; internal set; }

        public virtual IServiceHubCloner Cloner => Services.Cloner;

        public virtual IMetadataController MetadataController => Services.MetadataController;

        public virtual IWebClient WebClient => Services.WebClient;

        public virtual ICacheController CacheController => Services.CacheController;

        public virtual IParseObjectClassController ClassController => Services.ClassController;

        public virtual IParseInstallationController InstallationController => Services.InstallationController;

        public virtual IParseCommandRunner CommandRunner => Services.CommandRunner;

        public virtual IParseCloudCodeController CloudCodeController => Services.CloudCodeController;

        public virtual IParseConfigurationController ConfigurationController => Services.ConfigurationController;

        public virtual IParseFileController FileController => Services.FileController;

        public virtual IParseObjectController ObjectController => Services.ObjectController;

        public virtual IParseQueryController QueryController => Services.QueryController;

        public virtual IParseSessionController SessionController => Services.SessionController;

        public virtual IParseUserController UserController => Services.UserController;

        public virtual IParseCurrentUserController CurrentUserController => Services.CurrentUserController;

        public virtual IParseAnalyticsController AnalyticsController => Services.AnalyticsController;

        public virtual IParseInstallationCoder InstallationCoder => Services.InstallationCoder;

        public virtual IParsePushChannelsController PushChannelsController => Services.PushChannelsController;

        public virtual IParsePushController PushController => Services.PushController;

        public virtual IParseCurrentInstallationController CurrentInstallationController => Services.CurrentInstallationController;

        public virtual IServerConnectionData ServerConnectionData => Services.ServerConnectionData;

        public virtual IParseDataDecoder Decoder => Services.Decoder;

        public virtual IParseInstallationDataFinalizer InstallationDataFinalizer => Services.InstallationDataFinalizer;
    }
}
