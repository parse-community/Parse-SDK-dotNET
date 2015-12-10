// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace Parse.Internal {
  static class ReflectionHelpers {
    internal static IEnumerable<PropertyInfo> GetProperties(Type type) {
#if MONO || UNITY
      return type.GetProperties();
#else
      return type.GetRuntimeProperties();
#endif
    }

    internal static MethodInfo GetMethod(Type type, string name, Type[] parameters) {
#if MONO || UNITY
      return type.GetMethod(name, parameters);
#else
      return type.GetRuntimeMethod(name, parameters);
#endif
    }

    internal static bool IsPrimitive(Type type) {
#if MONO
			return type.IsPrimitive;
#else
      return type.GetTypeInfo().IsPrimitive;
#endif
    }

    internal static IEnumerable<Type> GetInterfaces(Type type) {
#if MONO || UNITY
      return type.GetInterfaces();
#else
      return type.GetTypeInfo().ImplementedInterfaces;
#endif
    }

    internal static bool IsConstructedGenericType(Type type) {
#if UNITY
      return type.IsGenericType && !type.IsGenericTypeDefinition;
#else
      return type.IsConstructedGenericType;
#endif
    }

    internal static IEnumerable<ConstructorInfo> GetConstructors(Type type) {
#if UNITY
      BindingFlags searchFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
      return type.GetConstructors(searchFlags);
#else
      return type.GetTypeInfo().DeclaredConstructors
        .Where(c => (c.Attributes & MethodAttributes.Static) == 0);
#endif
    }

    internal static Type[] GetGenericTypeArguments(Type type) {
#if UNITY
      return type.GetGenericArguments();
#else
      return type.GenericTypeArguments;
#endif
    }

    internal static PropertyInfo GetProperty(Type type, string name) {
#if MONO || UNITY
      return type.GetProperty(name);
#else
      return type.GetRuntimeProperty(name);
#endif
    }

    /// <summary>
    /// This method helps simplify the process of getting a constructor for a type.
    /// A method like this exists in .NET but is not allowed in a Portable Class Library,
    /// so we've built our own.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="parameterTypes"></param>
    /// <returns></returns>
    internal static ConstructorInfo FindConstructor(this Type self, params Type[] parameterTypes) {
      var constructors =
        from constructor in GetConstructors(self)
        let parameters = constructor.GetParameters()
        let types = from p in parameters select p.ParameterType
        where types.SequenceEqual(parameterTypes)
        select constructor;
      return constructors.SingleOrDefault();
    }

    internal static bool IsNullable(Type t) {
      bool isGeneric;
#if UNITY
      isGeneric = t.IsGenericType && !t.IsGenericTypeDefinition;
#else
      isGeneric = t.IsConstructedGenericType;
#endif
      return isGeneric && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>));
    }
  }
}

