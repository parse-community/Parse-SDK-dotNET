// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Parse.Utilities;

namespace Parse.Core.Internal {
  public class ParseAddOperation : IParseFieldOperation {
    private ReadOnlyCollection<object> objects;
    public ParseAddOperation(IEnumerable<object> objects) {
      this.objects = new ReadOnlyCollection<object>(objects.ToList());
    }

    public object Encode() {
      return new Dictionary<string, object> {
        {"__op", "Add"},
        {"objects", PointerOrLocalIdEncoder.Instance.Encode(objects)}
      };
    }

    public IParseFieldOperation MergeWithPrevious(IParseFieldOperation previous) {
      if (previous == null) {
        return this;
      }
      if (previous is ParseDeleteOperation) {
        return new ParseSetOperation(objects.ToList());
      }
      if (previous is ParseSetOperation) {
        var setOp = (ParseSetOperation)previous;
        var oldList = Conversion.To<IList<object>>(setOp.Value);
        return new ParseSetOperation(oldList.Concat(objects).ToList());
      }
      if (previous is ParseAddOperation) {
        return new ParseAddOperation(((ParseAddOperation)previous).Objects.Concat(objects));
      }
      throw new InvalidOperationException("Operation is invalid after previous operation.");
    }

    public object Apply(object oldValue, string key) {
      if (oldValue == null) {
        return objects.ToList();
      }
      var oldList = Conversion.To<IList<object>>(oldValue);
      return oldList.Concat(objects).ToList();
    }

    public IEnumerable<object> Objects {
      get {
        return objects;
      }
    }
  }
}
