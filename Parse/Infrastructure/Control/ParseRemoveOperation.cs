using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Infrastructure.Data;
using Parse.Infrastructure.Utilities;

namespace Parse.Infrastructure.Control;

public class ParseRemoveOperation : IParseFieldOperation
{
    // Read-only collection to ensure immutability
    ReadOnlyCollection<object> Data { get; }

    public ParseRemoveOperation(IEnumerable<object> objects) =>
        Data = new ReadOnlyCollection<object>(objects.Distinct().ToList()); // Ensure unique elements

    public IParseFieldOperation MergeWithPrevious(IParseFieldOperation previous)
    {
        return previous switch
        {
            null => this,
            ParseDeleteOperation _ => previous, // Retain delete operation
            ParseSetOperation setOp => new ParseSetOperation(
                Apply(Conversion.As<IList<object>>(setOp.Value), default)), // Remove items from existing value
            ParseRemoveOperation oldOp => new ParseRemoveOperation(
                oldOp.Objects.Concat(Data).Distinct()), // Combine unique removals
            _ => throw new InvalidOperationException("Operation is invalid after previous operation.")
        };
    }

    public object Apply(object oldValue, string key)
    {
        // Remove the specified objects from the old value
        return oldValue is { }
            ? Conversion.As<IList<object>>(oldValue).Except(Data, ParseFieldOperations.ParseObjectComparer).ToList()
            : new List<object> { }; // Return empty list if no previous value
    }

    public IDictionary<string, object> ConvertToJSON(IServiceHub serviceHub = default)
    {
        // Convert data to a JSON-compatible structure
        var encodedObjects = Data.Select(obj => PointerOrLocalIdEncoder.Instance.Encode(obj, serviceHub)).ToList();

        return new Dictionary<string, object>
        {
            ["__op"] = "Remove", // Parse operation type
            ["objects"] = encodedObjects
        };
    }

    public IEnumerable<object> Objects => Data;

    // Implemented Value property to expose the underlying data
    public object Value => Data.ToList();
}
