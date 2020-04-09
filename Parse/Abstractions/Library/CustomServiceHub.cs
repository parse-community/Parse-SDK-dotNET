using Parse.Analytics.Internal;
using Parse.Common.Internal;
using Parse.Core.Internal;
using Parse.Push.Internal;

namespace Parse.Abstractions.Library
{
    public abstract class CustomServiceHub : ICustomServiceHub
    {
        public virtual IServiceHub Services { get; internal set; }

        public virtual IServiceHubCloner Cloner => Services.Cloner;

        public virtual IMetadataController MetadataController => Services.MetadataController;

        public virtual IWebClient WebClient => Services.WebClient;

        public virtual IStorageController StorageController => Services.StorageController;

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

        public virtual IServerConnectionData ServerConnectionData { set; get; }

        public virtual IParseDataDecoder Decoder { set; get; }

        public virtual IParseInstallationDataFinalizer InstallationDataFinalizer { set; get; }
    }
}
