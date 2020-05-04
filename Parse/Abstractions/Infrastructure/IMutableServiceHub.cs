#pragma warning disable CS0108 // Member hides inherited member; missing new keyword

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
    public interface IMutableServiceHub : IServiceHub
    {
        IServerConnectionData ServerConnectionData { set; }
        IMetadataController MetadataController { set; }

        IServiceHubCloner Cloner { set; }

        IWebClient WebClient { set; }
        ICacheController CacheController { set; }
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
