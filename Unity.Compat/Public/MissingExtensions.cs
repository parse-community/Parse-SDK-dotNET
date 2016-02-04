using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace System {
  public static class MissingExtensions {
    public static bool HasFlag(this Enum enumValue, Enum flag) {
      var eInt = Convert.ToInt64(enumValue);
      var flagInt = Convert.ToInt64(flag);
      return (eInt & flagInt) == flagInt;
    }

    public static T GetCustomAttribute<T>(this PropertyInfo prop, bool inherit)
        where T : Attribute {
      return (T)prop.GetCustomAttributes(typeof(T), inherit).FirstOrDefault();
    }

    public static T GetCustomAttribute<T>(this PropertyInfo prop) where T : Attribute {
      return prop.GetCustomAttribute<T>(true);
    }

    public static T GetCustomAttribute<T>(this MemberInfo member, bool inherit)
        where T : Attribute {
          return (T)member.GetCustomAttributes(typeof(T), inherit).FirstOrDefault();
    }

    public static T GetCustomAttribute<T>(this MemberInfo member) where T : Attribute {
      return member.GetCustomAttribute<T>(true);
    }

    public static IEnumerable<TResult> Zip<T1, T2, TResult>(this IEnumerable<T1> list1,
        IEnumerable<T2> list2,
        Func<T1, T2, TResult> zipper) {
      var e1 = list1.GetEnumerator();
      var e2 = list2.GetEnumerator();
      while (e1.MoveNext() && e2.MoveNext()) {
        yield return zipper(e1.Current, e2.Current);
      }
    }
  }
}
