#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

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
    // TODO: Consider splitting up IServiceHub into IResourceHub and IServiceHub, where the former would provide the current functionality of IServiceHub and the latter would be a public-facing sub-section containing formerly-static memebers from classes such as ParseObject which require the use of some broader resource.

    /// <summary>
    /// The dependency injection container for all internal .NET Parse SDK services.
    /// </summary>
    public interface IServiceHub
    {
        /// <summary>
        /// The current server connection data that the the Parse SDK has been initialized with.
        /// </summary>
        IServerConnectionData ServerConnectionData { get; }
        IMetadataController MetadataController { get; }

        IServiceHubCloner Cloner { get; }

        IWebClient WebClient { get; }
        ICacheController CacheController { get; }
        IParseObjectClassController ClassController { get; }

        IParseDataDecoder Decoder { get; }

        IParseInstallationController InstallationController { get; }
        IParseCommandRunner CommandRunner { get; }

        IParseCloudCodeController CloudCodeController { get; }
        IParseConfigurationController ConfigurationController { get; }
        IParseFileController FileController { get; }
        IParseObjectController ObjectController { get; }
        IParseQueryController QueryController { get; }
        IParseSessionController SessionController { get; }
        IParseUserController UserController { get; }
        IParseCurrentUserController CurrentUserController { get; }

        IParseAnalyticsController AnalyticsController { get; }

        IParseInstallationCoder InstallationCoder { get; }

        IParsePushChannelsController PushChannelsController { get; }
        IParsePushController PushController { get; }
        IParseCurrentInstallationController CurrentInstallationController { get; }
        IParseInstallationDataFinalizer InstallationDataFinalizer { get; }
    }
}
