using System;
using System.Collections.Generic;
using System.Linq;

using Parse.Abstractions.Infrastructure.Control;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Abstractions.Platform.Objects;

namespace Parse.Infrastructure.Control;

public class ParseObjectIdComparer : IEqualityComparer<object>
{
    bool IEqualityComparer<object>.Equals(object p1, object p2)
    {
        ParseObject parseObj1 = p1 as ParseObject;
        ParseObject parseObj2 = p2 as ParseObject;
        if (parseObj1 != null && parseObj2 != null)
        {
            return Equals(parseObj1.ObjectId, parseObj2.ObjectId);
        }
        return Equals(p1, p2);
    }

    public int GetHashCode(object p)
    {
        ParseObject parseObject = p as ParseObject;
        if (parseObject != null)
        {
            return parseObject.ObjectId.GetHashCode();
        }
        return p.GetHashCode();
    }
}

static class ParseFieldOperations
{
    private static readonly ParseObjectIdComparer comparer = new();
    public static IEqualityComparer<object> ParseObjectComparer => comparer;

    /// <summary>
    /// The factory method for creating IParseFieldOperation instances from JSON.
    /// </summary>
    /// <param name="json">The JSON dictionary representing the operation.</param>
    /// <param name="decoder">The decoder to be used for nested objects.</param>
    /// <returns>A concrete IParseFieldOperation.</returns>
    public static IParseFieldOperation Decode(IDictionary<string, object> json, IParseDataDecoder decoder
        , IParseObjectClassController classController)
    {
        string opName = json["__op"] as string;

        switch (opName)
        {
            case "Delete":
                return ParseDeleteOperation.Instance;

            case "Increment":
                return new ParseIncrementOperation(json["amount"]);

            case "Add":
            case "AddUnique":
            case "Remove":
                var objects = (json["objects"] as IEnumerable<object>)
                    .Select(item => decoder.Decode(item, null)) // Recursively decode each item
                    .ToList();
                return opName switch
                {
                    "Add" => new ParseAddOperation(objects),
                    "AddUnique" => new ParseAddUniqueOperation(objects),
                    "Remove" => new ParseRemoveOperation(objects),
                    _ => null // Should not happen
                };

            case "AddRelation":
            case "RemoveRelation":
                var relationObjects = (json["objects"] as IEnumerable<object>)
                    .Select(item => decoder.Decode(item, null) as ParseObject)
                    .ToList();
                string targetClass = relationObjects.FirstOrDefault()?.ClassName;
                var adds = opName == "AddRelation" ? relationObjects : new List<ParseObject>();
                var removes = opName == "RemoveRelation" ? relationObjects : new List<ParseObject>();
                return new ParseRelationOperation(classController, adds, removes);

            case "Batch":
                var allAdds = new List<ParseObject>();
                var allRemoves = new List<ParseObject>();
                foreach (var op in json["ops"] as IEnumerable<object>)
                {
                    var opJson = op as IDictionary<string, object>;
                    string innerOpName = opJson["__op"] as string;
                    var innerObjects = (opJson["objects"] as IEnumerable<object>)
                        .Select(item => decoder.Decode(item, null) as ParseObject)
                        .ToList();

                    if (innerOpName == "AddRelation")
                        allAdds.AddRange(innerObjects);
                    if (innerOpName == "RemoveRelation")
                        allRemoves.AddRange(innerObjects);
                }
                return new ParseRelationOperation(classController, allAdds, allRemoves);

            default:
                throw new NotSupportedException($"Decoding for operation '{opName}' is not supported.");
        }
    }





}
