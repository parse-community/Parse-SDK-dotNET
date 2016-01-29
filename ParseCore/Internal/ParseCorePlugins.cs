// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Parse.Common.Internal;
using Parse.Core.Internal;

namespace Parse.Core.Internal {
  public class ParseCorePlugins : IParseCorePlugins {
    private static readonly object instanceMutex = new object();
    private static IParseCorePlugins instance;
    public static IParseCorePlugins Instance {
      get {
        lock (instanceMutex) {
          instance = instance ?? new ParseCorePlugins();
          return instance;
        }
      }
      set {
        lock (instanceMutex) {
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

    public void Reset() {
      lock (mutex) {
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

    public IHttpClient HttpClient {
      get {
        lock (mutex) {
          httpClient = httpClient ?? new HttpClient();
          return httpClient;
        }
      }
      set {
        lock (mutex) {
          httpClient = value;
        }
      }
    }

    public IParseCommandRunner CommandRunner {
      get {
        lock (mutex) {
          commandRunner = commandRunner ?? new ParseCommandRunner(HttpClient, InstallationIdController);
          return commandRunner;
        }
      }
      set {
        lock (mutex) {
          commandRunner = value;
        }
      }
    }

    public IStorageController StorageController {
      get {
        lock (mutex) {
          storageController = storageController ?? new StorageController();
          return storageController;
        }
      }
      set {
        lock (mutex) {
          storageController = value;
        }
      }
    }

    public IParseCloudCodeController CloudCodeController {
      get {
        lock (mutex) {
          cloudCodeController = cloudCodeController ?? new ParseCloudCodeController(CommandRunner);
          return cloudCodeController;
        }
      }
      set {
        lock (mutex) {
          cloudCodeController = value;
        }
      }
    }

    public IParseFileController FileController {
      get {
        lock (mutex) {
          fileController = fileController ?? new ParseFileController(CommandRunner);
          return fileController;
        }
      }
      set {
        lock (mutex) {
          fileController = value;
        }
      }
    }

    public IParseConfigController ConfigController {
      get {
        lock (mutex) {
          if (configController == null) {
            configController = new ParseConfigController(CommandRunner, StorageController);
          }
          return configController;
        }
      }
      set {
        lock (mutex) {
          configController = value;
        }
      }
    }

    public IParseObjectController ObjectController {
      get {
        lock (mutex) {
          objectController = objectController ?? new ParseObjectController(CommandRunner);
          return objectController;
        }
      }
      set {
        lock (mutex) {
          objectController = value;
        }
      }
    }

    public IParseQueryController QueryController {
      get {
        lock (mutex) {
          if (queryController == null) {
            queryController = new ParseQueryController(CommandRunner);
          }
          return queryController;
        }
      }
      set {
        lock (mutex) {
          queryController = value;
        }
      }
    }

    public IParseSessionController SessionController {
      get {
        lock (mutex) {
          sessionController = sessionController ?? new ParseSessionController(CommandRunner);
          return sessionController;
        }
      }
     set {
        lock (mutex) {
          sessionController = value;
        }
      }
    }

    public IParseUserController UserController {
      get {
        lock (mutex) {
          userController = userController ?? new ParseUserController(CommandRunner);
          return userController;
        }
      }
      set {
        lock (mutex) {
          userController = value;
        }
      }
    }

    public IParseCurrentUserController CurrentUserController {
      get {
        lock (mutex) {
          currentUserController = currentUserController ?? new ParseCurrentUserController(StorageController);
          return currentUserController;
        }
      }
      set {
        lock (mutex) {
          currentUserController = value;
        }
      }
    }

    public IObjectSubclassingController SubclassingController {
      get {
        lock (mutex) {
          if (subclassingController == null) {
            subclassingController = new ObjectSubclassingController();
            subclassingController.AddRegisterHook(typeof(ParseUser), () => CurrentUserController.ClearFromMemory());
          }
          return subclassingController;
        }
      }
      set {
        lock (mutex) {
          subclassingController = value;
        }
      }
    }

    public IInstallationIdController InstallationIdController {
      get {
        lock (mutex) {
          installationIdController = installationIdController ?? new InstallationIdController(StorageController);
          return installationIdController;
        }
      }
      set {
        lock (mutex) {
          installationIdController = value;
        }
      }
    }
  }
}
