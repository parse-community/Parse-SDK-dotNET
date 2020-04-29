// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Platform.Configuration;

namespace Parse.Abstractions.Platform.Configuration
{
    public interface IParseConfigurationController
    {
        public IParseCurrentConfigurationController CurrentConfigurationController { get; }

        /// <summary>
        /// Fetches the config from the server asynchronously.
        /// </summary>
        /// <returns>The config async.</returns>
        /// <param name="sessionToken">Session token.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<ParseConfiguration> FetchConfigAsync(string sessionToken, IServiceHub serviceHub, CancellationToken cancellationToken = default);
    }
}
