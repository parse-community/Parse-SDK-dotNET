using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Infrastructure.Data;
using Parse.Infrastructure.Utilities;

namespace Parse.Infrastructure.Control;

public class ParseAddOperation : IParseFieldOperation
{
    // Encapsulated the data to be added as a read-only collection
    ReadOnlyCollection<object> Data { get; }

    public ParseAddOperation(IEnumerable<object> objects) =>
        Data = new ReadOnlyCollection<object>(objects.Distinct().ToList()); // Ensures no duplicates within this operation

    public IParseFieldOperation MergeWithPrevious(IParseFieldOperation previous)
    {
        return previous switch
        {
            null => this,
            ParseDeleteOperation _ => new ParseSetOperation(Data.ToList()), // If deleted, replace with the new data
            ParseSetOperation setOp => new ParseSetOperation(
                Conversion.To<IList<object>>(setOp.Value).Concat(Data).ToList()), // Combine with existing data
            ParseAddOperation addition => new ParseAddOperation(
                addition.Objects.Concat(Data).Distinct()), // Merge and remove duplicates
            _ => throw new InvalidOperationException("Operation is invalid after previous operation.")
        };
    }

    public object Apply(object oldValue, string key)
    {
        if (oldValue == null)
        {
            return Data.ToList(); // Initialize the value as the data
        }

        var result = Conversion.To<IList<object>>(oldValue).ToList();
        foreach (var obj in Data)
        {
            if (!result.Contains(obj)) // Ensure no duplicates
            {
                result.Add(obj);
            }
        }
        return result;
    }

    public IDictionary<string, object> ConvertToJSON(IServiceHub serviceHub = default)
    {
        // Convert the data into JSON-compatible structures
        var encodedObjects = Data.Select(EncodeForParse).ToList();

        return new Dictionary<string, object>
        {
            ["__op"] = "Add",
            ["objects"] = encodedObjects
        };
    }

    private object EncodeForParse(object obj)
    {
        return obj switch
        {
            // Handle pointers
            ParseObject parseObj => new Dictionary<string, object>
            {
                ["__type"] = "Pointer",
                ["className"] = parseObj.ClassName,
                ["objectId"] = parseObj.ObjectId
            },

            // Handle GeoPoints
            ParseGeoPoint geoPoint => new Dictionary<string, object>
            {
                ["__type"] = "GeoPoint",
                ["latitude"] = geoPoint.Latitude,
                ["longitude"] = geoPoint.Longitude
            },

            // Handle Files
            ParseFile file => new Dictionary<string, object>
            {
                ["__type"] = "File",
                ["name"] = file.Name,
                ["url"] = file.Url
            },

            // Handle Relations
            ParseRelationBase relation => new Dictionary<string, object>
            {
                ["__type"] = "Relation",
                ["className"] = relation.TargetClassName
            },

            // Handle primitive types
            string or int or long or float or double or decimal or bool => obj,

            // Handle Bytes
            byte[] bytes => new Dictionary<string, object>
            {
                ["__type"] = "Bytes",
                ["base64"] = Convert.ToBase64String(bytes)
            },

            // Handle nested objects (JSON-like structure)
            IDictionary<string, object> nestedObject => nestedObject.ToDictionary(k => k.Key, k => EncodeForParse(k.Value)),

            // Handle arrays
            IEnumerable<object> array => array.Select(EncodeForParse).ToList(),

            // For unsupported types, throw an error
            _ => throw new InvalidOperationException($"Unsupported type: {obj.GetType()}")
        };
    }

    public IEnumerable<object> Objects => Data;

    // Added Value property to return the underlying data
    public object Value => Data.ToList();
}
