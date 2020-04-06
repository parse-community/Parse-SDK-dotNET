#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections.Generic;
using System.Text;
using Parse.Analytics.Internal;
using Parse.Common.Internal;
using Parse.Core.Internal;
using Parse.Push.Internal;

namespace Parse.Abstractions.Library
{
    // TODO: Consider splitting up IServiceHub into IResourceHub and IServiceHub, where the former would provide the current functionality of IServiceHub and the latter would be a public-facing sub-section containing formerly-static memebers from classes such as ParseObject which require the use of some broader resource.

    /// <summary>
    /// The dependency injection container for all internal .NET Parse SDK services.
    /// </summary>
    public interface IServiceHub
    {
        IServerConnectionData ServerConnectionData { get; }
        IMetadataController MetadataController { get; }

        IServiceHubCloner Cloner { get; }

        IWebClient WebClient { get; }
        IStorageController StorageController { get; }
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
