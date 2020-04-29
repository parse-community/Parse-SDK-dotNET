// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Objects;

namespace Parse.Abstractions.Platform.Sessions
{
    public interface IParseSessionController
    {
        Task<IObjectState> GetSessionAsync(string sessionToken, IServiceHub serviceHub, CancellationToken cancellationToken = default);

        Task RevokeAsync(string sessionToken, CancellationToken cancellationToken = default);

        Task<IObjectState> UpgradeToRevocableSessionAsync(string sessionToken, IServiceHub serviceHub, CancellationToken cancellationToken = default);

        bool IsRevocableSessionToken(string sessionToken);
    }
}
