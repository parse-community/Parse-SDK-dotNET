// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using LeanCloud.Core.Internal;
using System;

namespace LeanCloud.Analytics.Internal {
  public interface IAVAnalyticsPlugins {
    void Reset();

    IAVCorePlugins CorePlugins { get; }
    IAVAnalyticsController AnalyticsController { get; }
  }
}