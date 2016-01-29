// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Linq;
using System.Collections.Generic;

namespace Parse.Core.Internal {
  public class MutableObjectState : IObjectState {
    public bool IsNew { get; set; }
    public string ClassName { get; set; }
    public string ObjectId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CreatedAt { get; set; }

    // Initialize serverData to avoid further null checking.
    private IDictionary<string, object> serverData = new Dictionary<string, object>();
    public IDictionary<string, object> ServerData {
      get {
        return serverData;
      }

      set {
        serverData = value;
      }
    }

    public object this[string key] {
      get {
        return ServerData[key];
      }
    }

    public bool ContainsKey(string key) {
      return ServerData.ContainsKey(key);
    }

    public void Apply(IDictionary<string, IParseFieldOperation> operationSet) {
      // Apply operationSet
      foreach (var pair in operationSet) {
        object oldValue;
        ServerData.TryGetValue(pair.Key, out oldValue);
        var newValue = pair.Value.Apply(oldValue, pair.Key);
        if (newValue != ParseDeleteOperation.DeleteToken) {
          ServerData[pair.Key] = newValue;
        } else {
          ServerData.Remove(pair.Key);
        }
      }
    }

    public void Apply(IObjectState other) {
      IsNew = other.IsNew;
      if (other.ObjectId != null) {
        ObjectId = other.ObjectId;
      }
      if (other.UpdatedAt != null) {
        UpdatedAt = other.UpdatedAt;
      }
      if (other.CreatedAt != null) {
        CreatedAt = other.CreatedAt;
      }

      foreach (var pair in other) {
        ServerData[pair.Key] = pair.Value;
      }
    }

    public IObjectState MutatedClone(Action<MutableObjectState> func) {
      var clone = MutableClone();
      func(clone);
      return clone;
    }

    protected virtual MutableObjectState MutableClone() {
      return new MutableObjectState {
        IsNew = IsNew,
        ClassName = ClassName,
        ObjectId = ObjectId,
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt,
        ServerData = this.ToDictionary(t => t.Key, t => t.Value)
      };
    }

    IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator() {
      return ServerData.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
      return ((IEnumerable<KeyValuePair<string, object>>)this).GetEnumerator();
    }
  }
}
