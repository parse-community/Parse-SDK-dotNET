using System;
using System.Collections.Generic;
using System.Diagnostics;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Abstractions.Platform.Objects;
using Parse.Platform.Objects;

namespace Parse.Infrastructure.Data;

// TODO: (richardross) refactor entire parse coder interfaces.
// Done: (YB) though, I wonder why Encode is never used in the ParseObjectCoder class. Might update if I find a use case.
//Got it now. The Encode method is used in ParseObjectController.cs


/// <summary>
/// Provides methods to encode and decode Parse objects.
/// </summary>
public class ParseObjectCoder
{
    /// <summary>
    /// Gets the singleton instance of the ParseObjectCoder.
    /// </summary>
    public static ParseObjectCoder Instance { get; } = new ParseObjectCoder();

    // Private constructor to prevent external instantiation
    private ParseObjectCoder() { }

    /// <summary>
    /// Encodes the object state and operations using the provided encoder.
    /// </summary>
    public IDictionary<string, object> Encode<T>(
        T state,
        IDictionary<string, IParseFieldOperation> operations,
        ParseDataEncoder encoder,
        IServiceHub serviceHub
    ) where T : IObjectState
    {
        var result = new Dictionary<string, object>();

        foreach (var pair in operations)
        {
            var operation = pair.Value;
            result[pair.Key] = encoder.Encode(operation, serviceHub);
        }

        return result;
    }
    /// <summary>
    /// Decodes raw server data into a mutable object state.
    /// </summary>
    public IObjectState Decode(IDictionary<string, object> data, IParseDataDecoder decoder, IServiceHub serviceHub)
    {

        var serverData = new Dictionary<string, object>();
        var mutableData = new Dictionary<string, object>(data);

        // Extract key properties (existing logic)
        var objectId = Extract(mutableData, "objectId", obj => obj as string);
        var email = Extract(mutableData, "email", obj => obj as string);
        var username = Extract(mutableData, "username", obj => obj as string);
        var sessionToken = Extract(mutableData, "sessionToken", obj => obj as string);
        var error = Extract(mutableData, "error", obj => obj as string);
        var code = Extract(mutableData, "code", obj => Convert.ToInt32(obj));
        var emailVerified = Extract(mutableData, "emailVerified", obj => obj is bool value && value);

        var createdAt = Extract(mutableData, "createdAt", obj => ParseDataDecoder.ParseDate(obj as string));
        var updatedAt = Extract(mutableData, "updatedAt", obj => ParseDataDecoder.ParseDate(obj as string)) ?? createdAt;

        // Handle ACL extraction
        var acl = Extract(mutableData, "ACL", obj =>
        {
            if (obj is IDictionary<string, object> aclData)
            {
                return new ParseACL(aclData); // Return ParseACL if the format is correct
            }

            return null; // If ACL is missing or in an incorrect format, return null
        });

        if (acl != null)
        {
            serverData["ACL"] = acl; // Add the decoded ACL back to serverData
        }


        // Decode remaining fields
        foreach (var pair in mutableData)
        {
            if (pair.Key == "__type" || pair.Key == "className")
                continue;

            serverData[pair.Key] = decoder.Decode(pair.Value, serviceHub);
        }

        // Populate server data with primary properties
        PopulateServerData(serverData, "username", username);
        PopulateServerData(serverData, "email", email);
        PopulateServerData(serverData, "sessionToken", sessionToken);
        PopulateServerData(serverData, "error", error);
        PopulateServerData(serverData, "code", code);
        PopulateServerData(serverData, "emailVerified", emailVerified);

        return new MutableObjectState
        {
            ObjectId = objectId,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            ServerData = serverData,
            SessionToken = sessionToken
        };
    }

    /// <summary>
    /// Extracts a value from a dictionary and removes the key.
    /// </summary>
    private static T Extract<T>(IDictionary<string, object> data, string key, Func<object, T> action)
{
    if (data.TryGetValue(key, out var value))
    {
        data.Remove(key);
        return action(value);
    }

    return default;
}

/// <summary>
/// Populates server data with a value if not already present.
/// </summary>
private static void PopulateServerData(IDictionary<string, object> serverData, string key, object value)
{
    if (value != null && !serverData.ContainsKey(key))
    {
        serverData[key] = value;
    }
}
}
