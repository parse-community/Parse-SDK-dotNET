// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

namespace Parse.Core.Internal {
  public class ParseSetOperation : IParseFieldOperation {
    public ParseSetOperation(object value) {
      Value = value;
    }

    public object Encode() {
      return PointerOrLocalIdEncoder.Instance.Encode(Value);
    }

    public IParseFieldOperation MergeWithPrevious(IParseFieldOperation previous) {
      return this;
    }

    public object Apply(object oldValue, string key) {
      return Value;
    }

    public object Value { get; private set; }
  }
}
