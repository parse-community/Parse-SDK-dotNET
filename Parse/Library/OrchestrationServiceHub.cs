using System;
using System.Collections.Generic;
using System.Text;
using Parse.Abstractions.Library;
using Parse.Analytics.Internal;
using Parse.Common.Internal;
using Parse.Core.Internal;
using Parse.Push.Internal;

namespace Parse.Library
{
    public class OrchestrationServiceHub : IServiceHub
    {
        public IServiceHub Default { get; set; }

        public IServiceHub Custom { get; set; }

        public IServiceHubCloner Cloner => Custom.Cloner ?? Default.Cloner;

        public IMetadataController MetadataController => Custom.MetadataController ?? Default.MetadataController;

        public IWebClient WebClient => Custom.WebClient ?? Default.WebClient;

        public IStorageController StorageController => Custom.StorageController ?? Default.StorageController;

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
