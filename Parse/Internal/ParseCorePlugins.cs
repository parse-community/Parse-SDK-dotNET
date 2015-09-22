// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Parse.Internal;

namespace Parse.Internal {
  internal class ParseCorePlugins {
    private static readonly ParseCorePlugins instance = new ParseCorePlugins();
    public static ParseCorePlugins Instance {
      get {
        return instance;
      }
    }

    private readonly object mutex = new object();

    #region Server Controllers

    private IParseAnalyticsController analyticsController;
    private IParseCloudCodeController cloudCodeController;
    private IParseConfigController configController;
    private IParseFileController fileController;
    private IParseObjectController objectController;
    private IParseQueryController queryController;
    private IParseSessionController sessionController;
    private IParseUserController userController;
    private IParsePushController pushController;
    private IParsePushChannelsController pushChannelsController;

    #endregion

    #region Current Instance Controller

    private IInstallationIdController installationIdController;
    private IParseCurrentInstallationController currentInstallationController;
    private IParseCurrentUserController currentUserController;

    #endregion

    internal void Reset() {
      lock (mutex) {
        AnalyticsController = null;
        CloudCodeController = null;
        FileController = null;
        ObjectController = null;
        SessionController = null;
        UserController = null;

        CurrentInstallationController = null;
        CurrentUserController = null;
      }
    }

    public IParseAnalyticsController AnalyticsController {
      get {
        lock (mutex) {
          analyticsController = analyticsController ?? new ParseAnalyticsController(ParseClient.ParseCommandRunner);
          return analyticsController;
        }
      }
      internal set {
        lock (mutex) {
          analyticsController = value;
        }
      }
    }

    public IParseCloudCodeController CloudCodeController {
      get {
        lock (mutex) {
          cloudCodeController = cloudCodeController ?? new ParseCloudCodeController(ParseClient.ParseCommandRunner);
          return cloudCodeController;
        }
      }
      internal set {
        lock (mutex) {
          cloudCodeController = value;
        }
      }
    }

    public IParseFileController FileController {
      get {
        lock (mutex) {
          fileController = fileController ?? new ParseFileController(ParseClient.ParseCommandRunner);
          return fileController;
        }
      }
      internal set {
        lock (mutex) {
          fileController = value;
        }
      }
    }

    public IParseConfigController ConfigController {
      get {
        lock (mutex) {
          if (configController == null) {
            configController = new ParseConfigController();
          }
          return configController;
        }
      }
      internal set {
        lock (mutex) {
          configController = value;
        }
      }
    }

    public IParseObjectController ObjectController {
      get {
        lock (mutex) {
          if (objectController == null) {
            objectController = new ParseObjectController();
          }
          return objectController;
        }
      }
      internal set {
        lock (mutex) {
          objectController = value;
        }
      }
    }

    public IParseQueryController QueryController {
      get {
        lock (mutex) {
          if (queryController == null) {
            queryController = new ParseQueryController();
          }
          return queryController;
        }
      }

      internal set {
        lock (mutex) {
          queryController = value;
        }
      }
    }

    public IParseSessionController SessionController {
      get {
        lock (mutex) {
          sessionController = sessionController ?? new ParseSessionController(ParseClient.ParseCommandRunner);
          return sessionController;
        }
      }

      internal set {
        lock (mutex) {
          sessionController = value;
        }
      }
    }

    public IParseUserController UserController {
      get {
        lock (mutex) {
          userController = userController ?? new ParseUserController(ParseClient.ParseCommandRunner);
          return userController;
        }
      }
      internal set {
        lock (mutex) {
          userController = value;
        }
      }
    }

    public IParsePushController PushController {
      get {
        lock (mutex) {
          pushController = pushController ?? new ParsePushController();
          return pushController;
        }
      }
      internal set {
        lock (mutex) {
          pushController = value;
        }
      }
    }

    public IParsePushChannelsController PushChannelsController {
      get {
        lock (mutex) {
          pushChannelsController = pushChannelsController ?? new ParsePushChannelsController();
          return pushChannelsController;
        }
      }
      internal set {
        lock (mutex) {
          pushChannelsController = value;
        }
      }
    }

    public IInstallationIdController InstallationIdController {
      get {
        lock (mutex) {
          installationIdController = installationIdController ?? new InstallationIdController();
          return installationIdController;
        }
      }
      internal set {
        lock (mutex) {
          installationIdController = value;
        }
      }
    }

    public IParseCurrentInstallationController CurrentInstallationController {
      get {
        lock (mutex) {
          if (currentInstallationController == null) {
            currentInstallationController = new ParseCurrentInstallationController(InstallationIdController);
          }
          return currentInstallationController;
        }
      }
      internal set {
        lock (mutex) {
          currentInstallationController = value;
        }
      }
    }

    public IParseCurrentUserController CurrentUserController {
      get {
        lock (mutex) {
          currentUserController = currentUserController ?? new ParseCurrentUserController();
          return currentUserController;
        }
      }
      internal set {
        lock (mutex) {
          currentUserController = value;
        }
      }
    }
  }
}
