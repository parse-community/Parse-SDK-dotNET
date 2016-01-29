// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parse.Core.Internal {
  public class ParseRelationOperation : IParseFieldOperation {
    private readonly IList<string> adds;
    private readonly IList<string> removes;
    private readonly string targetClassName;

    private ParseRelationOperation(IEnumerable<string> adds,
        IEnumerable<string> removes,
        string targetClassName) {
      this.targetClassName = targetClassName;
      this.adds = new ReadOnlyCollection<string>(adds.ToList());
      this.removes = new ReadOnlyCollection<string>(removes.ToList());
    }

    public ParseRelationOperation(IEnumerable<ParseObject> adds,
        IEnumerable<ParseObject> removes) {
      adds = adds ?? new ParseObject[0];
      removes = removes ?? new ParseObject[0];
      this.targetClassName = adds.Concat(removes).Select(o => o.ClassName).FirstOrDefault();
      this.adds = new ReadOnlyCollection<string>(IdsFromObjects(adds).ToList());
      this.removes = new ReadOnlyCollection<string>(IdsFromObjects(removes).ToList());
    }

    public object Encode() {
      var adds = this.adds
          .Select(id => PointerOrLocalIdEncoder.Instance.Encode(
              ParseObject.CreateWithoutData(targetClassName, id)))
          .ToList();
      var removes = this.removes
          .Select(id => PointerOrLocalIdEncoder.Instance.Encode(
              ParseObject.CreateWithoutData(targetClassName, id)))
          .ToList();
      var addDict = adds.Count == 0 ? null : new Dictionary<string, object> {
        {"__op", "AddRelation"},
        {"objects", adds}
      };
      var removeDict = removes.Count == 0 ? null : new Dictionary<string, object> {
        {"__op", "RemoveRelation"},
        {"objects", removes}
      };

      if (addDict != null && removeDict != null) {
        return new Dictionary<string, object> {
          {"__op", "Batch"},
          {"ops", new[] {addDict, removeDict}}
        };
      }
      return addDict ?? removeDict;
    }

    public IParseFieldOperation MergeWithPrevious(IParseFieldOperation previous) {
      if (previous == null) {
        return this;
      }
      if (previous is ParseDeleteOperation) {
        throw new InvalidOperationException("You can't modify a relation after deleting it.");
      }
      var other = previous as ParseRelationOperation;
      if (other != null) {
        if (other.TargetClassName != TargetClassName) {
          throw new InvalidOperationException(
              string.Format("Related object must be of class {0}, but {1} was passed in.",
                  other.TargetClassName,
                  TargetClassName));
        }
        var newAdd = adds.Union(other.adds.Except(removes)).ToList();
        var newRemove = removes.Union(other.removes.Except(adds)).ToList();
        return new ParseRelationOperation(newAdd, newRemove, TargetClassName);
      }
      throw new InvalidOperationException("Operation is invalid after previous operation.");
    }

    public object Apply(object oldValue, string key) {
      if (adds.Count == 0 && removes.Count == 0) {
        return null;
      }
      if (oldValue == null) {
        return ParseRelationBase.CreateRelation(null, key, targetClassName);
      }
      if (oldValue is ParseRelationBase) {
        var oldRelation = (ParseRelationBase)oldValue;
        var oldClassName = oldRelation.TargetClassName;
        if (oldClassName != null && oldClassName != targetClassName) {
          throw new InvalidOperationException("Related object must be a " + oldClassName
              + ", but a " + targetClassName + " was passed in.");
        }
        oldRelation.TargetClassName = targetClassName;
        return oldRelation;
      }
      throw new InvalidOperationException("Operation is invalid after previous operation.");
    }

    public string TargetClassName { get { return targetClassName; } }

    private IEnumerable<string> IdsFromObjects(IEnumerable<ParseObject> objects) {
      foreach (var obj in objects) {
        if (obj.ObjectId == null) {
          throw new ArgumentException(
            "You can't add an unsaved ParseObject to a relation.");
        }
        if (obj.ClassName != targetClassName) {
          throw new ArgumentException(string.Format(
              "Tried to create a ParseRelation with 2 different types: {0} and {1}",
                  targetClassName,
                  obj.ClassName));
        }
      }
      return objects.Select(o => o.ObjectId).Distinct();
    }
  }
}
