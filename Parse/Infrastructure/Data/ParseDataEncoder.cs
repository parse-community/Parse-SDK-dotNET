using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Control;
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
        return value is null || value.GetType().IsPrimitive || value is string || value is ParseObject || value is ParseACL || value is ParseFile || value is ParseGeoPoint || value is ParseRelationBase || value is DateTime || value is byte[] || Conversion.As<IDictionary<string, object>>(value) is { } || Conversion.As<IList<object>>(value) is { };
    }

    // If this object has a special encoding, encode it and return the encoded object. Otherwise, just return the original object.
    public object Encode(object value, IServiceHub serviceHub)
    {
        return value switch
        {
            DateTime date => EncodeDate(date),
            byte[] bytes => EncodeBytes(bytes),
            ParseObject entity => EncodeObject(entity),
            IJsonConvertible jsonConvertible => jsonConvertible.ConvertToJSON(),
            IDictionary<string, object> dictionary => EncodeDictionary(dictionary, serviceHub),
            IList<object> list => EncodeList(list, serviceHub),
            IParseFieldOperation fieldOperation => EncodeFieldOperation(fieldOperation, serviceHub),
            _ => value
        };
    }

    protected abstract IDictionary<string, object> EncodeObject(ParseObject value);

    private static IDictionary<string, object> EncodeDate(DateTime date)
    {
        return new Dictionary<string, object>
        {
            ["iso"] = date.ToString(SupportedDateFormats.First(), CultureInfo.InvariantCulture),
            ["__type"] = "Date"
        };
    }

    private static IDictionary<string, object> EncodeBytes(byte[] bytes)
    {
        return new Dictionary<string, object>
        {
            ["__type"] = "Bytes",
            ["base64"] = Convert.ToBase64String(bytes)
        };
    }

    private object EncodeDictionary(IDictionary<string, object> dictionary, IServiceHub serviceHub)
    {
        return dictionary.ToDictionary(pair => pair.Key, pair => Encode(pair.Value, serviceHub));
    }

    private object EncodeList(IList<object> list, IServiceHub serviceHub)
    {
        if (ParseClient.IL2CPPCompiled && list.GetType().IsArray)
        {
            list = new List<object>(list);
        }

        List<object> encoded = new();

        foreach (object item in list)
        {
            if (!Validate(item))
            {
                throw new ArgumentException($"Invalid type for value in list: {item?.GetType().FullName}");
            }

            encoded.Add(Encode(item, serviceHub));
        }

        return encoded;
    }

    private object EncodeFieldOperation(IParseFieldOperation fieldOperation, IServiceHub serviceHub)
    {
        // Converting IParseFieldOperation to JSON (IJsonConvertible implementation Previous Todo - Done!)
        if (fieldOperation is IJsonConvertible jsonConvertible)
        {
            return jsonConvertible.ConvertToJSON();
        }

        throw new InvalidOperationException($"Field operation {fieldOperation.GetType().Name} does not implement IJsonConvertible.");
    }
}

