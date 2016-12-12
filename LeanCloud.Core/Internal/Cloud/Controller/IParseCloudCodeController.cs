// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Core.Internal
{
    public interface IAVCloudCodeController
    {
        Task<T> CallFunctionAsync<T>(String name,
            IDictionary<string, object> parameters,
            string sessionToken,
            CancellationToken cancellationToken);

        Task<T> RPCFunction<T>(string name, IDictionary<string, object> parameters,
            string sessionToken,
            CancellationToken cancellationToken);
    }
}
