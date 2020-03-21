// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse.Core.Internal;

namespace Parse.Analytics.Internal
{
    /// <summary>
    /// The controller for the Parse Analytics API.
    /// </summary>
    public class ParseAnalyticsController : IParseAnalyticsController
    {
        private IParseCommandRunner Runner { get; }

        /// <summary>
        /// Creates an instance of the Parse Analytics API controller.
        /// </summary>
        /// <param name="commandRunner">A <see cref="IParseCommandRunner"/> to use.</param>
        public ParseAnalyticsController(IParseCommandRunner commandRunner) => Runner = commandRunner;

        /// <summary>
        /// Tracks an event matching the specified details.
        /// </summary>
        /// <param name="name">The name of the event.</param>
        /// <param name="dimensions">The parameters of the event.</param>
        /// <param name="sessionToken">The session token for the event.</param>
        /// <param name="cancellationToken">The asynchonous cancellation token.</param>
        /// <returns>A <see cref="Task"/> that will complete successfully once the event has been set to be tracked.</returns>
        public Task TrackEventAsync(string name, IDictionary<string, string> dimensions, string sessionToken, CancellationToken cancellationToken)
        {
            IDictionary<string, object> data = new Dictionary<string, object>
            {
                ["at"] = DateTime.Now,
                ["name"] = name,
            };

            if (dimensions != null)
            {
                data["dimensions"] = dimensions;
            }

            return Runner.RunCommandAsync(new ParseCommand("events/" + name, "POST", sessionToken, data: PointerOrLocalIdEncoder.Instance.Encode(data) as IDictionary<string, object>), cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Tracks an app open for the specified event.
        /// </summary>
        /// <param name="pushHash">The hash for the target push notification.</param>
        /// <param name="sessionToken">The token of the current session.</param>
        /// <param name="cancellationToken">The asynchronous cancellation token.</param>
        /// <returns>A <see cref="Task"/> the will complete successfully once app openings for the target push notification have been set to be tracked.</returns>
        public Task TrackAppOpenedAsync(string pushHash, string sessionToken, CancellationToken cancellationToken)
        {
            IDictionary<string, object> data = new Dictionary<string, object> { ["at"] = DateTime.Now };

            if (pushHash != null)
            {
                data["push_hash"] = pushHash;
            }

            return Runner.RunCommandAsync(new ParseCommand("events/AppOpened", "POST", sessionToken, data: PointerOrLocalIdEncoder.Instance.Encode(data) as IDictionary<string, object>), cancellationToken: cancellationToken);
        }
    }
}
