using System;
using System.Collections.Generic;
using System.Text;
using Parse.Abstractions.Library;
using Parse.Abstractions.Management;
using Parse.Common.Internal;
using Parse.Core.Internal;

namespace Parse.Management
{
    /// <summary>
    /// Parse dependency injection container implemented as a bare-minimum value type so instances cost less; this is to be used when a container is needed to pass only a few dependencies to a dependent entity, and the main container is unaccessible, but the indiviudal requirements are.
    /// </summary>
    /// <remarks>This class has no implementation for <see cref="SetDefaults"/>.</remarks>
    public struct LightParseCorePlugins : IParseCorePlugins
    {
        public IMetadataController MetadataController { get; set; }

        public IWebClient WebClient { get; set; }

        public IStorageController StorageController { get; set; }

        public IParseObjectClassController SubclassingController { get; set; }

        public IParseInstallationController InstallationController { get; set; }

        public IParseCommandRunner CommandRunner { get; set; }

        public IParseCloudCodeController CloudCodeController { get; set; }

        public IParseConfigurationController ConfigController { get; set; }

        public IParseFileController FileController { get; set; }

        public IParseObjectController ObjectController { get; set; }

        public IParseQueryController QueryController { get; set; }

        public IParseSessionController SessionController { get; set; }

        public IParseUserController UserController { get; set; }

        public IParseCurrentUserController CurrentUserController { get; set; }

        public IParseCurrentConfigurationController CurrentConfigController { get; set; }

        public void Reset() => throw new NotImplementedException { };

        /// <summary>
        /// Will <see langword="throw"/> a <see cref="NotSupportedException"/>.
        /// </summary>
        public void SetDefaults() => throw new NotSupportedException { };
    }
}
