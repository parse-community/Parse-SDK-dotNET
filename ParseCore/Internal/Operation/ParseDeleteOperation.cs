// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System.Collections.Generic;

namespace LeanCloud.Core.Internal {
  /// <summary>
  /// An operation where a field is deleted from the object.
  /// </summary>
  public class AVDeleteOperation : IAVFieldOperation {
    internal static readonly object DeleteToken = new object();
    private static AVDeleteOperation _Instance = new AVDeleteOperation();
    public static AVDeleteOperation Instance {
      get {
        return _Instance;
      }
    }

    private AVDeleteOperation() { }
    public object Encode() {
      return new Dictionary<string, object> {
        {"__op", "Delete"}
      };
    }

    public IAVFieldOperation MergeWithPrevious(IAVFieldOperation previous) {
      return this;
    }

    public object Apply(object oldValue, string key) {
      return DeleteToken;
    }
  }
}
