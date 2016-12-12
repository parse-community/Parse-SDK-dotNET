// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Threading.Tasks;

namespace LeanCloud.Core.Internal {
  public interface IAVCurrentConfigController {
    /// <summary>
    /// Gets the current config async.
    /// </summary>
    /// <returns>The current config async.</returns>
    Task<AVConfig> GetCurrentConfigAsync();

    /// <summary>
    /// Sets the current config async.
    /// </summary>
    /// <returns>The current config async.</returns>
    /// <param name="config">Config.</param>
    Task SetCurrentConfigAsync(AVConfig config);

    /// <summary>
    /// Clears the current config async.
    /// </summary>
    /// <returns>The current config async.</returns>
    Task ClearCurrentConfigAsync();

    /// <summary>
    /// Clears the current config in memory async.
    /// </summary>
    /// <returns>The current config in memory async.</returns>
    Task ClearCurrentConfigInMemoryAsync();
  }
}
