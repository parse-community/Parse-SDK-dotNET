// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Platform.Configuration;

namespace Parse.Abstractions.Platform.Configuration
{
    public interface IParseCurrentConfigurationController
    {
        /// <summary>
        /// Gets the current config async.
        /// </summary>
        /// <returns>The current config async.</returns>
        Task<ParseConfiguration> GetCurrentConfigAsync(IServiceHub serviceHub);

        /// <summary>
        /// Sets the current config async.
        /// </summary>
        /// <returns>The current config async.</returns>
        /// <param name="config">Config.</param>
        Task SetCurrentConfigAsync(ParseConfiguration config);

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
