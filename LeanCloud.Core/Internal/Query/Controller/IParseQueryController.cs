// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Core.Internal {
  public interface IAVQueryController {
    Task<IEnumerable<IObjectState>> FindAsync<T>(AVQuery<T> query,
        AVUser user,
        CancellationToken cancellationToken) where T : AVObject;

    Task<int> CountAsync<T>(AVQuery<T> query,
        AVUser user,
        CancellationToken cancellationToken) where T : AVObject;

    Task<IObjectState> FirstAsync<T>(AVQuery<T> query,
        AVUser user,
        CancellationToken cancellationToken) where T : AVObject;
  }
}
