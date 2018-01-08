// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Parse.Utilities;

namespace Parse.Core.Internal {
  public class ParseRemoveOperation : IParseFieldOperation {
    private ReadOnlyCollection<object> objects;
    public ParseRemoveOperation(IEnumerable<object> objects) {
      this.objects = new ReadOnlyCollection<object>(objects.Distinct().ToList());
    }

    public object Encode() {
      return new Dictionary<string, object> {
        {"__op", "Remove"},
        {"objects", PointerOrLocalIdEncoder.Instance.Encode(objects)}
      };
    }

    public IParseFieldOperation MergeWithPrevious(IParseFieldOperation previous) {
      if (previous == null) {
        return this;
      }
      if (previous is ParseDeleteOperation) {
        return previous;
      }
      if (previous is ParseSetOperation) {
        var setOp = (ParseSetOperation)previous;
        var oldList = Conversion.As<IList<object>>(setOp.Value);
        return new ParseSetOperation(this.Apply(oldList, null));
      }
      if (previous is ParseRemoveOperation) {
        var oldOp = (ParseRemoveOperation)previous;
        return new ParseRemoveOperation(oldOp.Objects.Concat(objects));
      }
      throw new InvalidOperationException("Operation is invalid after previous operation.");
    }

    public object Apply(object oldValue,string key) {
      if (oldValue == null) {
        return new List<object>();
      }
      var oldList = Conversion.As<IList<object>>(oldValue);
      return oldList.Except(objects, ParseFieldOperations.ParseObjectComparer).ToList();
    }

    public IEnumerable<object> Objects {
      get {
        return objects;
      }
    }
  }
}
