// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;

namespace Parse.Push.Internal
{
    public interface IPushState
    {
        ParseQuery<ParseInstallation> Query { get; }
        IEnumerable<string> Channels { get; }
        DateTime? Expiration { get; }
        TimeSpan? ExpirationInterval { get; }
        DateTime? PushTime { get; }
        IDictionary<string, object> Data { get; }
        String Alert { get; }

        IPushState MutatedClone(Action<MutablePushState> func);
    }
}
