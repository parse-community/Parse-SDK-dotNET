using System;

namespace System {
  /// <summary>
  /// Unity does not have an API for GetTypeInfo(), instead they expose most of the methods
  /// on System.Reflection.TypeInfo on the type itself. This poses a problem for compatibility
  /// with the rest of the C# world, as we expect the result of GetTypeInfo() to be an actual TypeInfo,
  /// as well as be able to be converted back to a type using AsType().
  ///
  /// This class simply implements some of the simple missing methods on Type to make it as API-compatible
  /// as possible to TypeInfo.
  /// </summary>
  internal static class TypeExtensions {
    public static Type AsType(this Type type) {
      return type;
    }
  }
}
