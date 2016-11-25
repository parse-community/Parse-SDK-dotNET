// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using LeanCloud.Core.Internal;

namespace LeanCloud.Analytics.Internal {
  public class AVAnalyticsPlugins : IAVAnalyticsPlugins {
    private static readonly object instanceMutex = new object();
    private static IAVAnalyticsPlugins instance;
    public static IAVAnalyticsPlugins Instance {
      get {
        lock (instanceMutex) {
          instance = instance ?? new AVAnalyticsPlugins();
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

    private IAVCorePlugins corePlugins;
    private IAVAnalyticsController analyticsController;

    public void Reset() {
      lock (mutex) {
        CorePlugins = null;
        AnalyticsController = null;
      }
    }

    public IAVCorePlugins CorePlugins {
      get {
        lock (mutex) {
          corePlugins = corePlugins ?? AVPlugins.Instance;
          return corePlugins;
        }
      }
      set {
        lock (mutex) {
          corePlugins = value;
        }
      }
    }

    public IAVAnalyticsController AnalyticsController {
      get {
        lock (mutex) {
          analyticsController = analyticsController ?? new AVAnalyticsController(CorePlugins.CommandRunner);
          return analyticsController;
        }
      }
      set {
        lock (mutex) {
          analyticsController = value;
        }
      }
    }
  }
}