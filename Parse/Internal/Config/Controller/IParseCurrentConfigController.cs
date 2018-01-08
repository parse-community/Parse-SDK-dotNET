// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Threading.Tasks;

namespace Parse.Core.Internal {
  public interface IParseCurrentConfigController {
    /// <summary>
    /// Gets the current config async.
    /// </summary>
    /// <returns>The current config async.</returns>
    Task<ParseConfig> GetCurrentConfigAsync();

    /// <summary>
    /// Sets the current config async.
    /// </summary>
    /// <returns>The current config async.</returns>
    /// <param name="config">Config.</param>
    Task SetCurrentConfigAsync(ParseConfig config);

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
