using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Infrastructure.Data;
using Parse.Infrastructure.Utilities;

namespace Parse.Infrastructure.Control;

public class ParseAddUniqueOperation : IParseFieldOperation
{
    // Read-only collection to store unique data
    ReadOnlyCollection<object> Data { get; }

    public ParseAddUniqueOperation(IEnumerable<object> objects) =>
        Data = new ReadOnlyCollection<object>(objects.Distinct().ToList());

    public IParseFieldOperation MergeWithPrevious(IParseFieldOperation previous)
    {
        return previous switch
        {
            null => this,
            ParseDeleteOperation _ => new ParseSetOperation(Data.ToList()), // Replace deleted value with current data
            ParseSetOperation setOp => new ParseSetOperation(Apply(Conversion.To<IList<object>>(setOp.Value), default)), // Merge with existing value
            ParseAddUniqueOperation addition => new ParseAddUniqueOperation(addition.Objects.Concat(Data).Distinct()), // Combine both unique sets
            _ => throw new InvalidOperationException("Operation is invalid after previous operation.")
        };
    }

    public object Apply(object oldValue, string key)
    {
        if (oldValue == null)
        {
            return Data.ToList(); // If no previous value, return the current data
        }

        var result = Conversion.To<IList<object>>(oldValue).ToList();
        var comparer = ParseFieldOperations.ParseObjectComparer;

        foreach (var target in Data)
        {
            // Add only if not already present, replace if an equivalent exists
            if (result.FirstOrDefault(reference => comparer.Equals(target, reference)) is { } matched)
            {
                result[result.IndexOf(matched)] = target;
            }
            else
            {
                result.Add(target);
            }
        }

        return result;
    }

    public IDictionary<string, object> ConvertToJSON(IServiceHub serviceHub = default)
    {
        // Converts the data into JSON-compatible structures
        var encodedObjects = Data.Select(EncodeForParse).ToList();

        return new Dictionary<string, object>
        {
            ["__op"] = "AddUnique", // Parse operation type
            ["objects"] = encodedObjects
        };
    }

    // Helper method for encoding individual objects
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
