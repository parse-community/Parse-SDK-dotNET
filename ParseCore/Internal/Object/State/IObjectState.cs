// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;

namespace Parse.Core.Internal {
  public interface IObjectState : IEnumerable<KeyValuePair<string, object>> {
    bool IsNew { get; }
    string ClassName { get; }
    string ObjectId { get; }
    DateTime? UpdatedAt { get; }
    DateTime? CreatedAt { get; }
    object this[string key] { get; }

    bool ContainsKey(string key);

    IObjectState MutatedClone(Action<MutableObjectState> func);
  }
}
