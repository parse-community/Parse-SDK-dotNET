#pragma warning disable CS0108 // Member hides inherited member; missing new keyword

using System;
using System.Collections.Generic;
using System.Text;
using Parse.Analytics.Internal;
using Parse.Common.Internal;
using Parse.Core.Internal;
using Parse.Push.Internal;

namespace Parse.Abstractions.Library
{
    public interface IMutableServiceHub : IServiceHub
    {
        IServerConnectionData ServerConnectionData { set; }
        IMetadataController MetadataController { set; }

        IServiceHubCloner Cloner { set; }

        IWebClient WebClient { set; }
        IStorageController StorageController { set; }
        IParseObjectClassController ClassController { set; }

        IParseDataDecoder Decoder { set; }

        IParseInstallationController InstallationController { set; }
        IParseCommandRunner CommandRunner { set; }

        IParseCloudCodeController CloudCodeController { set; }
        IParseConfigurationController ConfigurationController { set; }
        IParseFileController FileController { set; }
        IParseObjectController ObjectController { set; }
        IParseQueryController QueryController { set; }
        IParseSessionController SessionController { set; }
        IParseUserController UserController { set; }
        IParseCurrentUserController CurrentUserController { set; }

        IParseAnalyticsController AnalyticsController { set; }

        IParseInstallationCoder InstallationCoder { set; }

        IParsePushChannelsController PushChannelsController { set; }
        IParsePushController PushController { set; }
        IParseCurrentInstallationController CurrentInstallationController { set; }
        IParseInstallationDataFinalizer InstallationDataFinalizer { set; }
    }
}
