// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Parse.Utilities;

namespace Parse.Core.Internal {
  public class ParseAddUniqueOperation : IParseFieldOperation {
    private ReadOnlyCollection<object> objects;
    public ParseAddUniqueOperation(IEnumerable<object> objects) {
      this.objects = new ReadOnlyCollection<object>(objects.Distinct().ToList());
    }

    public object Encode() {
      return new Dictionary<string, object> {
        {"__op", "AddUnique"},
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
        var result = this.Apply(oldList, null);
        return new ParseSetOperation(result);
      }
      if (previous is ParseAddUniqueOperation) {
        var oldList = ((ParseAddUniqueOperation)previous).Objects;
        return new ParseAddUniqueOperation((IList<object>)this.Apply(oldList, null));
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
        if (objToAdd is ParseObject) {
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
