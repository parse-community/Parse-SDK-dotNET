// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Core.Internal {
  public interface IAVSessionController {
    Task<IObjectState> GetSessionAsync(string sessionToken, CancellationToken cancellationToken);

    Task RevokeAsync(string sessionToken, CancellationToken cancellationToken);

    Task<IObjectState> UpgradeToRevocableSessionAsync(string sessionToken, CancellationToken cancellationToken);

    bool IsRevocableSessionToken(string sessionToken);
  }
}
