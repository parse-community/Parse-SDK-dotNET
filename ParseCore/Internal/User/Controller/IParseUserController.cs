// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Parse.Core.Internal {
  public interface IParseUserController {
    Task<IObjectState> SignUpAsync(IObjectState state,
        IDictionary<string, IParseFieldOperation> operations,
        CancellationToken cancellationToken);

    Task<IObjectState> LogInAsync(string username,
        string password,
        CancellationToken cancellationToken);

    Task<IObjectState> LogInAsync(string authType,
        IDictionary<string, object> data,
        CancellationToken cancellationToken);

    Task<IObjectState> GetUserAsync(string sessionToken, CancellationToken cancellationToken);

    Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken);
  }
}
