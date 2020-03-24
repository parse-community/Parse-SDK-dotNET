// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using Parse.Abstractions.Library;
using Parse.Abstractions.Management;
using Parse.Common.Internal;
using Parse.Core.Internal;
using Parse.Library;

#if DEBUG
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Parse.Test")]
#endif

namespace Parse.Management
{
    public class ParseCorePlugins : IParseCorePlugins
    {
        static object Mutex { get; } = new object();
        static IParseCorePlugins instance;

        public static IParseCorePlugins Instance
        {
            get
            {
                lock (Mutex)
                    return instance ??= new ParseCorePlugins();
            }
            set
            {
                lock (Mutex)
                    instance = value;
            }
        }

        private readonly object mutex = new object();

        #region Server Controllers

        IMetadataController metadataController;

        IWebClient httpClient;
        IParseCommandRunner commandRunner;
        IStorageController storageController;

        IParseCloudCodeController cloudCodeController;
        IParseConfigController configController;
        IParseFileController fileController;
        IParseObjectController objectController;
        IParseQueryController queryController;
        IParseSessionController sessionController;
        IParseUserController userController;
        IObjectSubclassingController subclassingController;

        #endregion

        #region Current Instance Controller

        IParseCurrentUserController currentUserController;
        IParseInstallationController installationController;

        #endregion

        public void Reset()
        {
            lock (mutex)
            {
                MetadataController = null;
                WebClient = null;
                CommandRunner = null;
                StorageController = null;

                CloudCodeController = null;
                FileController = null;
                ObjectController = null;
                SessionController = null;
                UserController = null;
                SubclassingController = null;

                CurrentUserController = null;
                InstallationController = null;
            }
        }

        public IMetadataController MetadataController
        {
            get
            {
                lock (mutex)
                    return metadataController ??= new MetadataController { };
            }
            set
            {
                lock (mutex)
                    metadataController = value;
            }
        }

        public IWebClient WebClient
        {
            get
            {
                lock (mutex)
                    return httpClient ??= new UniversalWebClient { };
            }
            set
            {
                lock (mutex)
                    httpClient = value;
            }
        }

        public IParseCommandRunner CommandRunner
        {
            get
            {
                lock (mutex)
                    return commandRunner ??= new ParseCommandRunner(WebClient, InstallationController, MetadataController);
            }
            set
            {
                lock (mutex)
                    commandRunner = value;
            }
        }

        public IStorageController StorageController
        {
            get
            {
                lock (mutex)
                    return storageController ??= new StorageController();
            }
            set
            {
                lock (mutex)
                    storageController = value;
            }
        }

        public IParseCloudCodeController CloudCodeController
        {
            get
            {
                lock (mutex)
                    return cloudCodeController ??= new ParseCloudCodeController(CommandRunner);
            }
            set
            {
                lock (mutex)
                    cloudCodeController = value;
            }
        }

        public IParseFileController FileController
        {
            get
            {
                lock (mutex)
                    return fileController ??= new ParseFileController(CommandRunner);
            }
            set
            {
                lock (mutex)
                    fileController = value;
            }
        }

        public IParseConfigController ConfigController
        {
            get
            {
                lock (mutex)
                    return configController ?? (configController = new ParseConfigController(CommandRunner, StorageController));
            }
            set
            {
                lock (mutex)
                    configController = value;
            }
        }

        public IParseObjectController ObjectController
        {
            get
            {
                lock (mutex)
                    return objectController ??= new ParseObjectController(CommandRunner);
            }
            set
            {
                lock (mutex)
                    objectController = value;
            }
        }

        public IParseQueryController QueryController
        {
            get
            {
                lock (mutex)
                    return queryController ?? (queryController = new ParseQueryController(CommandRunner));
            }
            set
            {
                lock (mutex)
                    queryController = value;
            }
        }

        public IParseSessionController SessionController
        {
            get
            {
                lock (mutex)
                    return sessionController ??= new ParseSessionController(CommandRunner);
            }
            set
            {
                lock (mutex)
                    sessionController = value;
            }
        }

        public IParseUserController UserController
        {
            get
            {
                lock (mutex)
                    return userController ??= new ParseUserController(CommandRunner);
            }
            set
            {
                lock (mutex)
                    userController = value;
            }
        }

        public IParseCurrentUserController CurrentUserController
        {
            get
            {
                lock (mutex)
                    return currentUserController ??= new ParseCurrentUserController(StorageController);
            }
            set
            {
                lock (mutex)
                    currentUserController = value;
            }
        }

        public IObjectSubclassingController SubclassingController
        {
            get
            {
                lock (mutex)
                {
                    if (subclassingController == null)
                    {
                        subclassingController = new ObjectSubclassingController();
                        subclassingController.AddRegisterHook(typeof(ParseUser), () => CurrentUserController.ClearFromMemory());
                    }
                    return subclassingController;
                }
            }
            set
            {
                lock (mutex)
                    subclassingController = value;
            }
        }

        public IParseInstallationController InstallationController
        {
            get
            {
                lock (mutex)
                    return installationController ??= new ParseInstallationController(StorageController);
            }
            set
            {
                lock (mutex)
                    installationController = value;
            }
        }
    }
}
