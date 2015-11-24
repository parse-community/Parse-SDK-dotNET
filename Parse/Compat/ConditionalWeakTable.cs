using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Runtime.CompilerServices {
  internal class ConditionalWeakTable<TKey, TValue>
    where TKey : class
    where TValue : class {

    private class Reference {
      private int hashCode;
      public Reference(TKey obj) {
        this.hashCode = obj.GetHashCode();
        WeakReference = new WeakReference(obj);
      }
      public WeakReference WeakReference { get; private set; }

      public TKey Value {
        get {
          return (TKey)WeakReference.Target;
        }
      }

      public override int GetHashCode() {
        return hashCode;
      }

      public override bool Equals(object obj) {
        var otherRef = obj as Reference;
        if (otherRef == null) {
          return false;
        }
        if (otherRef.GetHashCode() != this.GetHashCode()) {
          return false;
        }
        return object.ReferenceEquals(otherRef.WeakReference.Target, WeakReference.Target);
      }
    }
    public delegate TValue CreateValueCallback(TKey key);
    private IDictionary<Reference, TValue> data;
    public ConditionalWeakTable() {
      data = new Dictionary<Reference, TValue>();
    }

    private void CleanUp() {
      foreach (var k in new HashSet<Reference>(data.Keys)) {
        if (!k.WeakReference.IsAlive) {
          data.Remove(k);
        }
      }
    }

    public TValue GetValue(TKey key, CreateValueCallback createValueCallback) {
      CleanUp();
      var reference = new Reference(key);
      TValue value;
      if (data.TryGetValue(reference, out value)) {
        return value;
      }
      return data[reference] = createValueCallback(key);
    }
  }
}
