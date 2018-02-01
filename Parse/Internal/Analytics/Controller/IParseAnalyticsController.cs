// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Parse.Analytics.Internal
{
    public interface IParseAnalyticsController
    {
        Task TrackEventAsync(string name,
            IDictionary<string, string> dimensions,
            string sessionToken,
            CancellationToken cancellationToken);

        Task TrackAppOpenedAsync(string pushHash,
            string sessionToken,
            CancellationToken cancellationToken);
    }
}
