// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using Parse.Common.Internal;

namespace Parse.Utilities
{
    /// <summary>
    /// A set of utilities for converting generic types between each other.
    /// </summary>
    public static class Conversion
    {
        /// <summary>
        /// Converts a value to the requested type -- coercing primitives to
        /// the desired type, wrapping lists and dictionaries appropriately,
        /// or else returning null.
        ///
        /// This should be used on any containers that might be coming from a
        /// user to normalize the collection types. Collection types coming from
        /// JSON deserialization can be safely assumed to be lists or dictionaries of
        /// objects.
        /// </summary>
        public static T As<T>(object value) where T : class => ConvertTo<T>(value) as T;

        /// <summary>
        /// Converts a value to the requested type -- coercing primitives to
        /// the desired type, wrapping lists and dictionaries appropriately,
        /// or else throwing an exception.
        ///
        /// This should be used on any containers that might be coming from a
        /// user to normalize the collection types. Collection types coming from
        /// JSON deserialization can be safely assumed to be lists or dictionaries of
        /// objects.
        /// </summary>
        public static T To<T>(object value) => (T) ConvertTo<T>(value);

        /// <summary>
        /// Converts a value to the requested type -- coercing primitives to
        /// the desired type, wrapping lists and dictionaries appropriately,
        /// or else passing the object along to the caller unchanged.
        ///
        /// This should be used on any containers that might be coming from a
        /// user to normalize the collection types. Collection types coming from
        /// JSON deserialization can be safely assumed to be lists or dictionaries of
        /// objects.
        /// </summary>
        internal static object ConvertTo<T>(object value)
        {
            if (value is T || value == null)
            {
                return value;
            }

            if (ReflectionHelpers.IsPrimitive(typeof(T)))
            {
                return (T) Convert.ChangeType(value, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
            }

            if (ReflectionHelpers.IsConstructedGenericType(typeof(T)))
            {
                // Add lifting for nullables. Only supports conversions between primitives.
                if (ReflectionHelpers.IsNullable(typeof(T)))
                {
                    Type innerType = ReflectionHelpers.GetGenericTypeArguments(typeof(T))[0];
                    if (ReflectionHelpers.IsPrimitive(innerType))
                    {
                        return (T) Convert.ChangeType(value, innerType, System.Globalization.CultureInfo.InvariantCulture);
                    }
                }
                Type listType = GetInterfaceType(value.GetType(), typeof(IList<>));
                if (listType != null &&
                    typeof(T).GetGenericTypeDefinition() == typeof(IList<>))
                {
                    Type wrapperType = typeof(FlexibleListWrapper<,>)
                      .MakeGenericType(ReflectionHelpers.GetGenericTypeArguments(typeof(T))[0],
                                       ReflectionHelpers.GetGenericTypeArguments(listType)[0]);
                    return Activator.CreateInstance(wrapperType, value);
                }
                Type dictType = GetInterfaceType(value.GetType(), typeof(IDictionary<,>));
                if (dictType != null && typeof(T).GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    Type wrapperType = typeof(FlexibleDictionaryWrapper<,>)
                      .MakeGenericType(ReflectionHelpers.GetGenericTypeArguments(typeof(T))[1],
                                       ReflectionHelpers.GetGenericTypeArguments(dictType)[1]);
                    return Activator.CreateInstance(wrapperType, value);
                }
            }

            return value;
        }

        /// <summary>
        /// Holds a dictionary that maps a cache of interface types for related concrete types.
        /// The lookup is slow the first time for each type because it has to enumerate all interface
        /// on the object type, but made fast by the cache.
        ///
        /// The map is:
        ///    (object type, generic interface type) => constructed generic type
        /// </summary>
        private static readonly Dictionary<Tuple<Type, Type>, Type> interfaceLookupCache = new Dictionary<Tuple<Type, Type>, Type>();

        private static Type GetInterfaceType(Type objType, Type genericInterfaceType)
        {
            Tuple<Type, Type> cacheKey = new Tuple<Type, Type>(objType, genericInterfaceType);
            if (interfaceLookupCache.ContainsKey(cacheKey))
            {
                return interfaceLookupCache[cacheKey];
            }
            foreach (Type type in ReflectionHelpers.GetInterfaces(objType))
            {
                if (ReflectionHelpers.IsConstructedGenericType(type) && type.GetGenericTypeDefinition() == genericInterfaceType)
                {
                    return interfaceLookupCache[cacheKey] = type;
                }
            }
            return null;
        }
    }
}