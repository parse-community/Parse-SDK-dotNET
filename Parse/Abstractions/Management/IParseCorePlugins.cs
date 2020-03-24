// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using Parse.Abstractions.Library;
using Parse.Common.Internal;
using Parse.Core.Internal;

namespace Parse.Abstractions.Management
{
    /// <summary>
    /// The dependency injection container for the .NET Parse SDK.
    /// </summary>
    public interface IParseCorePlugins
    {
        IMetadataController MetadataController { get; }

        IWebClient WebClient { get; }
        IStorageController StorageController { get; }
        IObjectSubclassingController SubclassingController { get; }

        IParseInstallationController InstallationController { get; }
        IParseCommandRunner CommandRunner { get; }

        IParseCloudCodeController CloudCodeController { get; }
        IParseConfigController ConfigController { get; }
        IParseFileController FileController { get; }
        IParseObjectController ObjectController { get; }
        IParseQueryController QueryController { get; }
        IParseSessionController SessionController { get; }
        IParseUserController UserController { get; }
        IParseCurrentUserController CurrentUserController { get; }
        //IParseCurrentConfigController CurrentConfigController { get; }

        public void Reset();

        /// <summary>
        /// Sets the default controller instances if not explicitly overridden. This method should effectively perform a <see langword="null"/>-coalescing assign on all of the properties of the <see cref="IParseCorePlugins"/> implementation instance.
        /// </summary>
        //public void SetDefaults();
    }
}