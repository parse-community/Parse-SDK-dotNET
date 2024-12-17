using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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
            value.GetType().IsPrimitive||
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
            // DateTime encoding
            DateTime date => EncodeDate(date),

            // Byte array encoding
            byte[] bytes => EncodeBytes(bytes),

            // ParseObject encoding
            ParseObject entity => EncodeObject(entity),

            // JSON-convertible types
            IJsonConvertible jsonConvertible => jsonConvertible.ConvertToJSON(serviceHub),

            // Dictionary encoding
            IDictionary<string, object> dictionary => EncodeDictionary(dictionary, serviceHub),
            IDictionary<string, string> dictionary => EncodeDictionary(dictionary, serviceHub),
            IDictionary<string, int> dictionary => EncodeDictionary(dictionary, serviceHub),
            IDictionary<string, long> dictionary => EncodeDictionary(dictionary, serviceHub),
            IDictionary<string, float> dictionary => EncodeDictionary(dictionary, serviceHub),
            IDictionary<string, double> dictionary => EncodeDictionary(dictionary, serviceHub),
            
            // List or array encoding
            IEnumerable<object> list => EncodeList(list, serviceHub),
            Array array => EncodeList(array.Cast<object>(), serviceHub),

            // Parse field operations
            

            // Primitive types or strings
            _ when value.GetType().IsPrimitive || value is string => value,

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
        if (dictionary.Count<1)
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


    // Add a specialized method to handle string-only dictionaries
    private object EncodeDictionary(IDictionary<string, string> dictionary, IServiceHub serviceHub)
    {
        
        return dictionary.ToDictionary(
            pair => pair.Key,
            pair => Encode(pair.Value, serviceHub) // Encode string values as object
        );
    }

    // Add a specialized method to handle int-only dictionaries
    private object EncodeDictionary(IDictionary<string, int> dictionary, IServiceHub serviceHub)
    {
        

        return dictionary.ToDictionary(
            pair => pair.Key,
            pair => Encode(pair.Value, serviceHub) // Encode int values as object
        );
    }

    // Add a specialized method to handle long-only dictionaries
    private object EncodeDictionary(IDictionary<string, long> dictionary, IServiceHub serviceHub)
    {
        

        return dictionary.ToDictionary(
            pair => pair.Key,
            pair => Encode(pair.Value, serviceHub) // Encode long values as object
        );
    }

    // Add a specialized method to handle float-only dictionaries
    private object EncodeDictionary(IDictionary<string, float> dictionary, IServiceHub serviceHub)
    {
        

        return dictionary.ToDictionary(
            pair => pair.Key,
            pair => Encode(pair.Value, serviceHub) // Encode float values as object
        );
    }

    // Add a specialized method to handle double-only dictionaries
    private object EncodeDictionary(IDictionary<string, double> dictionary, IServiceHub serviceHub)
    {
        

        return dictionary.ToDictionary(
            pair => pair.Key,
            pair => Encode(pair.Value, serviceHub) // Encode double values as object
        );
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




    /// <summary>
    /// Encodes a field operation into a JSON-compatible structure.
    /// </summary>
    private object EncodeFieldOperation(IParseFieldOperation fieldOperation, IServiceHub serviceHub)
    {
        if (fieldOperation is IJsonConvertible jsonConvertible)
        {
            return jsonConvertible.ConvertToJSON();
        }

        throw new InvalidOperationException($"Cannot encode field operation of type {fieldOperation.GetType().Name}.");
    }
}
