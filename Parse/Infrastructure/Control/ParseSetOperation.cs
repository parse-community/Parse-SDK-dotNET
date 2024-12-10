using System;
using System.Collections.Generic;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Infrastructure.Data;

namespace Parse.Infrastructure.Control;

public class ParseSetOperation : IParseFieldOperation
{
    public ParseSetOperation(object value) => Value = value;

    // Replace Encode with ConvertToJSON
    public IDictionary<string, object> ConvertToJSON(IServiceHub serviceHub = default)
    {
        if (serviceHub == null)
        {
            throw new InvalidOperationException("ServiceHub is required to encode the value.");
        }

        return PointerOrLocalIdEncoder.Instance.Encode(Value, serviceHub) switch
        {
            IDictionary<string, object> encodedValue => encodedValue,
            _ => new Dictionary<string, object>
            {
                ["__op"] = "Set",
                ["value"] = Value
            }
        };
    }


    public IParseFieldOperation MergeWithPrevious(IParseFieldOperation previous)
    {
        // Set operation always overrides previous operations
        return this;
    }

    public object Apply(object oldValue, string key)
    {
        // Set operation always sets the field to the specified value
        return Value;
    }

    public object Value { get; private set; }
}
