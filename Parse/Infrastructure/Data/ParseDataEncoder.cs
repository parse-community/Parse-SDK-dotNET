using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.ExceptionServices;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Infrastructure.Control;
using Parse.Infrastructure.Utilities;

namespace Parse.Infrastructure.Data;

/// <summary>
/// A <c>ParseEncoder</c> can be used to transform objects such as <see cref="ParseObject"/> into JSON
/// data structures.
/// </summary>
/// <seealso cref="ParseDataDecoder"/>
public abstract class ParseDataEncoder
{
    private static readonly string[] SupportedDateFormats = ParseClient.DateFormatStrings;

    public static bool Validate(object value)
    {
        return value is null ||
            value.GetType().IsPrimitive ||
            value is string ||
            value is ParseObject ||
            value is ParseACL ||
            value is ParseFile ||
            value is ParseGeoPoint ||
            value is ParseRelationBase ||
            value is DateTime ||
            value is byte[] ||
            value is Array ||
            Conversion.As<IDictionary<string, object>>(value) is { } ||
            Conversion.As<IDictionary<string, string>>(value) is { } ||
            Conversion.As<IDictionary<string, bool>>(value) is { } ||
            Conversion.As<IDictionary<string, Int32>>(value) is { } ||
            Conversion.As<IDictionary<string, float>>(value) is { } ||
            Conversion.As<IDictionary<string, long>>(value) is { } ||
            Conversion.As<IDictionary<string, double>>(value) is { } ||
            Conversion.As<IList<object>>(value) is { };
    }

    /// <summary>
    /// Encodes a given value into a JSON-compatible structure.
    /// </summary>
    public object Encode(object value, IServiceHub serviceHub)
    {
        if (value == null)
            return null;
        return value switch
        {
            // Primitive types or strings
            _ when value.GetType().IsPrimitive || value is string => value,
            // DateTime encoding
            DateTime date => EncodeDate(date),

            // Byte array encoding
            byte[] bytes => EncodeBytes(bytes),

            // ParseObject encoding
            ParseObject entity => EncodeObject(entity),

            // JSON-convertible types
            ParseSetOperation setOperation => setOperation.ConvertValueToJSON(serviceHub),
            IJsonConvertible jsonConvertible => jsonConvertible.ConvertToJSON(serviceHub),

            // Dictionary encoding
            IDictionary<string, object> dictionary => EncodeDictionary(dictionary, serviceHub),
            IDictionary<string, IDictionary<string, object>> dictionary => EncodeDictionaryStringDict(dictionary, serviceHub),
            // List or array encoding
            IEnumerable<object> list => EncodeList(list, serviceHub),
            Array array => EncodeList(array.Cast<object>(), serviceHub),



            // Unsupported types
            _ => throw new ArgumentException($"Unsupported type for encoding: {value?.GetType()?.FullName}")
        };
    }


    /// <summary>
    /// Encodes a ParseObject into a JSON-compatible structure.
    /// </summary>
    protected abstract IDictionary<string, object> EncodeObject(ParseObject value);

    /// <summary>
    /// Encodes a DateTime into a JSON-compatible structure.
    /// </summary>
    private static IDictionary<string, object> EncodeDate(DateTime date)
    {
        return new Dictionary<string, object>
        {

            ["iso"] = date.ToString(SupportedDateFormats.First(), CultureInfo.InvariantCulture),
            ["__type"] = "Date"
        };
    }

    /// <summary>
    /// Encodes a byte array into a JSON-compatible structure.
    /// </summary>
    private static IDictionary<string, object> EncodeBytes(byte[] bytes)
    {
        return new Dictionary<string, object>
        {
            ["__type"] = "Bytes",
            ["base64"] = Convert.ToBase64String(bytes)
        };
    }

    //// <summary>
    /// Encodes a dictionary into a JSON-compatible structure.
    /// </summary>
    private object EncodeDictionary(IDictionary<string, object> dictionary, IServiceHub serviceHub)
    {
        var encodedDictionary = new Dictionary<string, object>();
        if (dictionary.Count < 1)
        {
            return encodedDictionary;
        }
        foreach (var pair in dictionary)
        {
            // Check if the value is a Dictionary<string, string>
            if (pair.Value is IDictionary<string, string> stringDictionary)
            {
                // If the value is a Dictionary<string, string>, handle it separately
                encodedDictionary[pair.Key] = stringDictionary.ToDictionary(k => k.Key, v => (object) v.Value);
            }
            else
            {
                // Handle other types by encoding them recursively
                encodedDictionary[pair.Key] = Encode(pair.Value, serviceHub);
            }
        }

        return encodedDictionary;
    }
  
    // Add a specialized method to handle double-only dictionaries
    private object EncodeDictionaryStringDict(IDictionary<string, IDictionary<string, object>> dictionary, IServiceHub serviceHub)
    {
        return dictionary.ToDictionary(
        pair => pair.Key,
        pair =>
        {
            // If the value is another dictionary, recursively process it
            if (pair.Value is IDictionary<string, object> nestedDict)
            {
                return EncodeDictionary(nestedDict, serviceHub);
            }

            // Return the actual value as-is
            return pair.Value;
        });

    }



    /// <summary>
    /// Encodes a list into a JSON-compatible structure.
    /// </summary>
    private object EncodeList(IEnumerable<object> list, IServiceHub serviceHub)
    {


        List<object> encoded = new();
        foreach (var item in list)
        {
            if (item == null)
            {
                encoded.Add(null);
                continue;
            }

            if (!Validate(item))
            {
                throw new ArgumentException($"Invalid type for value in list: {item?.GetType().FullName}");
            }

            encoded.Add(Encode(item, serviceHub));
        }

        return encoded;
    }

}
