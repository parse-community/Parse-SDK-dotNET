//// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

//using Parse.Abstractions.Library;
//using Parse.Abstractions.Management;
//using Parse.Common.Internal;
//using Parse.Core.Internal;
//using Parse.Library;

//namespace Parse.Management
//{
//    public class ParseCorePlugins : IParseCorePlugins
//    {
//        //static object Mutex { get; } = new object { };
//        //static IParseCorePlugins instance;

//        //public static IParseCorePlugins Instance
//        //{
//        //    get
//        //    {
//        //        lock (Mutex)
//        //            return instance ??= new ParseCorePlugins { };
//        //    }
//        //    set
//        //    {
//        //        lock (Mutex)
//        //            instance = value;
//        //    }
//        //}

//        object InstanceMutex { get; } = new object { };

//        #region Server Controllers

//        IMetadataController metadataController;

//        IWebClient webClient;
//        IParseCommandRunner commandRunner;
//        IStorageController storageController;

//        IParseCloudCodeController cloudCodeController;
//        IParseConfigController configController;
//        IParseFileController fileController;
//        IParseObjectController objectController;
//        IParseQueryController queryController;
//        IParseSessionController sessionController;
//        IParseUserController userController;
//        IObjectSubclassingController subclassingController;

//        #endregion

//        #region Current Instance Controller

//        IParseCurrentUserController currentUserController;
//        IParseInstallationController installationController;

//        #endregion

//        public void Reset()
//        {
//            lock (InstanceMutex)
//            {
//                MetadataController = null;

//                WebClient = null;
//                CommandRunner = null;
//                StorageController = null;

//                CloudCodeController = null;
//                FileController = null;
//                ObjectController = null;
//                SessionController = null;
//                UserController = null;
//                SubclassingController = null;

//                CurrentUserController = null;
//                InstallationController = null;
//            }
//        }

//        public IMetadataController MetadataController
//        {
//            get
//            {
//                lock (InstanceMutex)
//                    return metadataController ??= new MetadataController { };
//            }
//            set
//            {
//                lock (InstanceMutex)
//                    metadataController = value;
//            }
//        }

//        public IWebClient WebClient
//        {
//            get
//            {
//                lock (InstanceMutex)
//                    return webClient ??= new UniversalWebClient { };
//            }
//            set
//            {
//                lock (InstanceMutex)
//                    webClient = value;
//            }
//        }

//        public IParseCommandRunner CommandRunner
//        {
//            get
//            {
//                lock (InstanceMutex)
//                    return commandRunner ??= new ParseCommandRunner(WebClient, InstallationController, MetadataController);
//            }
//            set
//            {
//                lock (InstanceMutex)
//                    commandRunner = value;
//            }
//        }

//        public IStorageController StorageController
//        {
//            get
//            {
//                lock (InstanceMutex)
//                    return storageController ??= new StorageController();
//            }
//            set
//            {
//                lock (InstanceMutex)
//                    storageController = value;
//            }
//        }

//        public IParseCloudCodeController CloudCodeController
//        {
//            get
//            {
//                lock (InstanceMutex)
//                    return cloudCodeController ??= new ParseCloudCodeController(CommandRunner);
//            }
//            set
//            {
//                lock (InstanceMutex)
//                    cloudCodeController = value;
//            }
//        }

//        public IParseFileController FileController
//        {
//            get
//            {
//                lock (InstanceMutex)
//                    return fileController ??= new ParseFileController(CommandRunner);
//            }
//            set
//            {
//                lock (InstanceMutex)
//                    fileController = value;
//            }
//        }

//        public IParseConfigController ConfigController
//        {
//            get
//            {
//                lock (InstanceMutex)
//                    return configController ??= new ParseConfigController(CommandRunner, StorageController);
//            }
//            set
//            {
//                lock (InstanceMutex)
//                    configController = value;
//            }
//        }

//        public IParseObjectController ObjectController
//        {
//            get
//            {
//                lock (InstanceMutex)
//                    return objectController ??= new ParseObjectController(CommandRunner);
//            }
//            set
//            {
//                lock (InstanceMutex)
//                    objectController = value;
//            }
//        }

//        public IParseQueryController QueryController
//        {
//            get
//            {
//                lock (InstanceMutex)
//                    return queryController ??= new ParseQueryController(CommandRunner);
//            }
//            set
//            {
//                lock (InstanceMutex)
//                    queryController = value;
//            }
//        }

//        public IParseSessionController SessionController
//        {
//            get
//            {
//                lock (InstanceMutex)
//                    return sessionController ??= new ParseSessionController(CommandRunner);
//            }
//            set
//            {
//                lock (InstanceMutex)
//                    sessionController = value;
//            }
//        }

//        public IParseUserController UserController
//        {
//            get
//            {
//                lock (InstanceMutex)
//                    return userController ??= new ParseUserController(CommandRunner);
//            }
//            set
//            {
//                lock (InstanceMutex)
//                    userController = value;
//            }
//        }

//        public IParseCurrentUserController CurrentUserController
//        {
//            get
//            {
//                lock (InstanceMutex)
//                    return currentUserController ??= new ParseCurrentUserController(StorageController);
//            }
//            set
//            {
//                lock (InstanceMutex)
//                    currentUserController = value;
//            }
//        }

//        public IObjectSubclassingController SubclassingController
//        {
//            get
//            {
//                lock (InstanceMutex)
//                {
//                    if (subclassingController == null)
//                    {
//                        subclassingController = new ObjectSubclassingController();
//                        subclassingController.AddRegisterHook(typeof(ParseUser), () => CurrentUserController.ClearFromMemory());
//                    }
//                    return subclassingController;
//                }
//            }
//            set
//            {
//                lock (InstanceMutex)
//                    subclassingController = value;
//            }
//        }

//        public IParseInstallationController InstallationController
//        {
//            get
//            {
//                lock (InstanceMutex)
//                    return installationController ??= new ParseInstallationController(StorageController);
//            }
//            set
//            {
//                lock (InstanceMutex)
//                    installationController = value;
//            }
//        }
//    }
//}
