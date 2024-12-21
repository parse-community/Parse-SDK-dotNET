using System.Collections.Generic;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Control;

namespace Parse.Infrastructure.Control;

/// <summary>
/// An operation where a field is deleted from the object.
/// </summary>
public class ParseDeleteOperation : IParseFieldOperation
{
    internal static object Token { get; } = new object { };

    public static ParseDeleteOperation Instance { get; } = new ParseDeleteOperation();

    public object Value => null; // Updated to return null as the value for delete operations

    private ParseDeleteOperation() { }

    // Replaced Encode with ConvertToJSON
    public IDictionary<string, object> ConvertToJSON(IServiceHub serviceHub = default)
    {
        return new Dictionary<string, object>
        {
            ["__op"] = "Delete"
        };
    }

    public IParseFieldOperation MergeWithPrevious(IParseFieldOperation previous)
    {
        // Merging with any previous operation results in this delete operation
        return this;
    }

    public object Apply(object oldValue, string key)
    {
        // When applied, delete the field by returning the delete token
        return Token;
    }

    
}
