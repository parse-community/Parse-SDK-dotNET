// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using LeanCloud.Utilities;

namespace LeanCloud.Core.Internal {
  public class AVRemoveOperation : IAVFieldOperation {
    private ReadOnlyCollection<object> objects;
    public AVRemoveOperation(IEnumerable<object> objects) {
      this.objects = new ReadOnlyCollection<object>(objects.Distinct().ToList());
    }

    public object Encode() {
      return new Dictionary<string, object> {
        {"__op", "Remove"},
        {"objects", PointerOrLocalIdEncoder.Instance.Encode(objects)}
      };
    }

    public IAVFieldOperation MergeWithPrevious(IAVFieldOperation previous) {
      if (previous == null) {
        return this;
      }
      if (previous is AVDeleteOperation) {
        return previous;
      }
      if (previous is AVSetOperation) {
        var setOp = (AVSetOperation)previous;
        var oldList = Conversion.As<IList<object>>(setOp.Value);
        return new AVSetOperation(this.Apply(oldList, null));
      }
      if (previous is AVRemoveOperation) {
        var oldOp = (AVRemoveOperation)previous;
        return new AVRemoveOperation(oldOp.Objects.Concat(objects));
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
