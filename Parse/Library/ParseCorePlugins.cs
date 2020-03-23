// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using Parse.Abstractions.Library;
using Parse.Common.Internal;
using Parse.Library;

#if DEBUG
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Parse.Test")]
#endif

namespace Parse.Core.Internal
{
    public class ParseCorePlugins : IParseCorePlugins
    {
        public IMetadataController MetadataController { get; set; }

        public IWebClient WebClient { get; set; }

        public IStorageController StorageController { get; set; }

        public IObjectSubclassingController SubclassingController { get; set; }

        public IParseInstallationController InstallationController { get; set; }

        public IParseCommandRunner CommandRunner { get; set; }

        public IParseCloudCodeController CloudCodeController { get; set; }

        public IParseConfigController ConfigController { get; set; }

        public IParseFileController FileController { get; set; }

        public IParseObjectController ObjectController { get; set; }

        public IParseQueryController QueryController { get; set; }

        public IParseSessionController SessionController { get; set; }

        public IParseUserController UserController { get; set; }

        public IParseCurrentUserController CurrentUserController { get; set; }

        public IParseCurrentConfigController CurrentConfigController { get; set; }

        // ALTERNATE NAME: InitializeWithDefaults

        public static ParseCorePlugins Instance { get; set; }

        public void Activate() => Instance = this;

        public void Reset() => (MetadataController, WebClient, StorageController, SubclassingController, InstallationController, CommandRunner, CloudCodeController, ConfigController, FileController, ObjectController, QueryController, SessionController, UserController, CurrentUserController, CurrentConfigController) = (default, default, default, default, default, default, default, default, default, default, default, default, default, default, default);

        public void SetDefaults()
        {
            MetadataController ??= new MetadataController { };

            WebClient ??= new UniversalWebClient { };
            StorageController ??= new StorageController { };
            SubclassingController ??= new ObjectSubclassingController { };

            InstallationController ??= new ParseInstallationController(StorageController);
            CommandRunner ??= new ParseCommandRunner(WebClient, InstallationController, MetadataController);

            CloudCodeController ??= new ParseCloudCodeController(CommandRunner);
            ConfigController ??= new ParseConfigController(CommandRunner, StorageController);
            FileController ??= new ParseFileController(CommandRunner);
            ObjectController ??= new ParseObjectController(CommandRunner);
            QueryController ??= new ParseQueryController(CommandRunner);
            SessionController ??= new ParseSessionController(CommandRunner);
            UserController ??= new ParseUserController(CommandRunner);
            CurrentUserController ??= new ParseCurrentUserController(StorageController);
            CurrentConfigController ??= new ParseCurrentConfigController(StorageController);
        }
    }
}
