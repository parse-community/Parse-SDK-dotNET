using System;
using System.Collections.Generic;

namespace Parse.Infrastructure.Utilities
{
#pragma warning disable CS1030 // #warning directive
#warning Possibly should be refactored.

    /// <summary>
    /// A set of utilities for converting generic types between each other.
    /// </summary>
    public static class Conversion
#pragma warning restore CS1030 // #warning directive
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
        public static T As<T>(object value) where T : class
        {
            return ConvertTo<T>(value) as T;
        }

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
        public static T To<T>(object value)
        {
            return (T) ConvertTo<T>(value);
        }

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
                return value;

            if (typeof(T).IsPrimitive)
                return (T) Convert.ChangeType(value, typeof(T), System.Globalization.CultureInfo.InvariantCulture);

            if (typeof(T).IsConstructedGenericType)
            {
                // Add lifting for nullables. Only supports conversions between primitives.

                if (typeof(T).CheckWrappedWithNullable() && typeof(T).GenericTypeArguments[0] is { IsPrimitive: true } innerType)
                    return (T) Convert.ChangeType(value, innerType, System.Globalization.CultureInfo.InvariantCulture);

                if (GetInterfaceType(value.GetType(), typeof(IList<>)) is { } listType && typeof(T).GetGenericTypeDefinition() == typeof(IList<>))
                    return Activator.CreateInstance(typeof(FlexibleListWrapper<,>).MakeGenericType(typeof(T).GenericTypeArguments[0], listType.GenericTypeArguments[0]), value);

                if (GetInterfaceType(value.GetType(), typeof(IDictionary<,>)) is { } dictType && typeof(T).GetGenericTypeDefinition() == typeof(IDictionary<,>))
                    return Activator.CreateInstance(typeof(FlexibleDictionaryWrapper<,>).MakeGenericType(typeof(T).GenericTypeArguments[1], dictType.GenericTypeArguments[1]), value);
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
        static Dictionary<Tuple<Type, Type>, Type> InterfaceLookupCache { get; } = new Dictionary<Tuple<Type, Type>, Type>();

        static Type GetInterfaceType(Type objType, Type genericInterfaceType)
        {
            Tuple<Type, Type> cacheKey = new Tuple<Type, Type>(objType, genericInterfaceType);

            if (InterfaceLookupCache.ContainsKey(cacheKey))
                return InterfaceLookupCache[cacheKey];

            foreach (Type type in objType.GetInterfaces())
                if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == genericInterfaceType)
                    return InterfaceLookupCache[cacheKey] = type;

            return default;
        }
    }
}