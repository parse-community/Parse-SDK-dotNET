// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Parse.Common.Internal;
using Parse.Core.Internal;

#if DEBUG
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Parse.Test")]
#endif

namespace Parse.Core.Internal
{
    public class ParseCorePlugins : IParseCorePlugins
    {
        private static readonly object instanceMutex = new object();
        private static IParseCorePlugins instance;
        public static IParseCorePlugins Instance
        {
            get
            {
                lock (instanceMutex)
                {
                    return instance = instance ?? new ParseCorePlugins();
                }
            }
            set
            {
                lock (instanceMutex)
                {
                    instance = value;
                }
            }
        }

        private readonly object mutex = new object();

        #region Server Controllers

        private IHttpClient httpClient;
        private IParseCommandRunner commandRunner;
        private IStorageController storageController;

        private IParseCloudCodeController cloudCodeController;
        private IParseConfigController configController;
        private IParseFileController fileController;
        private IParseObjectController objectController;
        private IParseQueryController queryController;
        private IParseSessionController sessionController;
        private IParseUserController userController;
        private IObjectSubclassingController subclassingController;

        #endregion

        #region Current Instance Controller

        private IParseCurrentUserController currentUserController;
        private IInstallationIdController installationIdController;

        #endregion

        public void Reset()
        {
            lock (mutex)
            {
                HttpClient = null;
                CommandRunner = null;
                StorageController = null;

                CloudCodeController = null;
                FileController = null;
                ObjectController = null;
                SessionController = null;
                UserController = null;
                SubclassingController = null;

                CurrentUserController = null;
                InstallationIdController = null;
            }
        }

        public IHttpClient HttpClient
        {
            get
            {
                lock (mutex)
                {
                    return httpClient = httpClient ?? new HttpClient();
                }
            }
            set
            {
                lock (mutex)
                {
                    httpClient = value;
                }
            }
        }

        public IParseCommandRunner CommandRunner
        {
            get
            {
                lock (mutex)
                {
                    return commandRunner = commandRunner ?? new ParseCommandRunner(HttpClient, InstallationIdController);
                }
            }
            set
            {
                lock (mutex)
                {
                    commandRunner = value;
                }
            }
        }

        public IStorageController StorageController
        {
            get
            {
                lock (mutex)
                {
                    return storageController = storageController ?? new StorageController();
                }
            }
            set
            {
                lock (mutex)
                {
                    storageController = value;
                }
            }
        }

        public IParseCloudCodeController CloudCodeController
        {
            get
            {
                lock (mutex)
                {
                    return cloudCodeController = cloudCodeController ?? new ParseCloudCodeController(CommandRunner);
                }
            }
            set
            {
                lock (mutex)
                {
                    cloudCodeController = value;
                }
            }
        }

        public IParseFileController FileController
        {
            get
            {
                lock (mutex)
                {
                    return fileController = fileController ?? new ParseFileController(CommandRunner);
                }
            }
            set
            {
                lock (mutex)
                {
                    fileController = value;
                }
            }
        }

        public IParseConfigController ConfigController
        {
            get
            {
                lock (mutex)
                {
                    return configController ?? (configController = new ParseConfigController(CommandRunner, StorageController));
                }
            }
            set
            {
                lock (mutex)
                {
                    configController = value;
                }
            }
        }

        public IParseObjectController ObjectController
        {
            get
            {
                lock (mutex)
                {
                    return objectController = objectController ?? new ParseObjectController(CommandRunner);
                }
            }
            set
            {
                lock (mutex)
                {
                    objectController = value;
                }
            }
        }

        public IParseQueryController QueryController
        {
            get
            {
                lock (mutex)
                {
                    return queryController ?? (queryController = new ParseQueryController(CommandRunner));
                }
            }
            set
            {
                lock (mutex)
                {
                    queryController = value;
                }
            }
        }

        public IParseSessionController SessionController
        {
            get
            {
                lock (mutex)
                {
                    return sessionController = sessionController ?? new ParseSessionController(CommandRunner);
                }
            }
            set
            {
                lock (mutex)
                {
                    sessionController = value;
                }
            }
        }

        public IParseUserController UserController
        {
            get
            {
                lock (mutex)
                {
                    return (userController = userController ?? new ParseUserController(CommandRunner));
                }
            }
            set
            {
                lock (mutex)
                {
                    userController = value;
                }
            }
        }

        public IParseCurrentUserController CurrentUserController
        {
            get
            {
                lock (mutex)
                {
                    return currentUserController = currentUserController ?? new ParseCurrentUserController(StorageController);
                }
            }
            set
            {
                lock (mutex)
                {
                    currentUserController = value;
                }
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
                {
                    subclassingController = value;
                }
            }
        }

        public IInstallationIdController InstallationIdController
        {
            get
            {
                lock (mutex)
                {
                    return installationIdController = installationIdController ?? new InstallationIdController(StorageController);
                }
            }
            set
            {
                lock (mutex)
                {
                    installationIdController = value;
                }
            }
        }
    }
}
