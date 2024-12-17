using System;
using System.Collections.Generic;

namespace Parse.Infrastructure.Data;

/// <summary>
/// A <see cref="ParseDataEncoder"/> that encodes <see cref="ParseObject"/> as pointers. If the object does not have an <see cref="ParseObject.ObjectId"/>, uses a local id.
/// </summary>
public class PointerOrLocalIdEncoder : ParseDataEncoder
{
    public static PointerOrLocalIdEncoder Instance { get; } = new PointerOrLocalIdEncoder { };

    protected override IDictionary<string, object> EncodeObject(ParseObject value)
    {
        if (value.ObjectId is null)
        {
            // TODO (hallucinogen): handle local id. For now we throw.

            throw new InvalidOperationException("Cannot create a pointer to an object without an objectId.");
        }

        return new Dictionary<string, object>
        {
            ["__type"] = "Pointer",
            ["className"] = value.ClassName,
            ["objectId"] = value.ObjectId
        };
    }
}
