using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System {
  internal static class Tuple {
    // This is useful because it allows for type inference, which normally cannot be done with constructors, but can be done for static methods.
    public static Tuple<T1, T2> Create<T1, T2>(T1 t1, T2 t2) {
      return new Tuple<T1, T2>(t1, t2);
    }
  }

  internal class Tuple<T1, T2> {
    public T1 Item1 { get; private set; }
    public T2 Item2 { get; private set; }
    public Tuple(T1 item1, T2 item2) {
      Item1 = item1;
      Item2 = item2;
    }

    public override bool Equals(object obj) {
      var other = obj as Tuple<T1, T2>;
      if (other == null) {
        return false;
      }
      return object.Equals(Item1, other.Item1) && object.Equals(Item2, other.Item2);
    }

    public override int GetHashCode() {
      int code1 = Item1 != null ? Item1.GetHashCode() : 0;
      int code2 = Item2 != null ? Item2.GetHashCode() : 0;
      return code1 ^ code2;
    }
  }
}
