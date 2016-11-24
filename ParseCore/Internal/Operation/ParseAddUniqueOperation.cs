// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LeanCloud.Utilities;

namespace LeanCloud.Core.Internal {
  public class AVAddUniqueOperation : IAVFieldOperation {
    private ReadOnlyCollection<object> objects;
    public AVAddUniqueOperation(IEnumerable<object> objects) {
      this.objects = new ReadOnlyCollection<object>(objects.Distinct().ToList());
    }

    public object Encode() {
      return new Dictionary<string, object> {
        {"__op", "AddUnique"},
        {"objects", PointerOrLocalIdEncoder.Instance.Encode(objects)}
      };
    }

    public IAVFieldOperation MergeWithPrevious(IAVFieldOperation previous) {
      if (previous == null) {
        return this;
      }
      if (previous is AVDeleteOperation) {
        return new AVSetOperation(objects.ToList());
      }
      if (previous is AVSetOperation) {
        var setOp = (AVSetOperation)previous;
        var oldList = Conversion.To<IList<object>>(setOp.Value);
        var result = this.Apply(oldList, null);
        return new AVSetOperation(result);
      }
      if (previous is AVAddUniqueOperation) {
        var oldList = ((AVAddUniqueOperation)previous).Objects;
        return new AVAddUniqueOperation((IList<object>)this.Apply(oldList, null));
      }
      throw new InvalidOperationException("Operation is invalid after previous operation.");
    }

    public object Apply(object oldValue, string key) {
      if (oldValue == null) {
        return objects.ToList();
      }
      var newList = Conversion.To<IList<object>>(oldValue).ToList();
      var comparer = ParseFieldOperations.ParseObjectComparer;
      foreach (var objToAdd in objects) {
        if (objToAdd is AVObject) {
          var matchedObj = newList.FirstOrDefault(listObj => comparer.Equals(objToAdd, listObj));
          if (matchedObj == null) {
            newList.Add(objToAdd);
          } else {
            var index = newList.IndexOf(matchedObj);
            newList[index] = objToAdd;
          }
        } else if (!newList.Contains<object>(objToAdd, comparer)) {
          newList.Add(objToAdd);
        }
      }
      return newList;
    }

    public IEnumerable<object> Objects {
      get {
        return objects;
      }
    }
  }
}
