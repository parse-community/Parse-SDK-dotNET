using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure.Control;

namespace Parse.Platform.Objects;

public class MutableObjectState : IObjectState
{
    public bool IsNew { get; set; }
    //public bool EmailVerified { get; set; }
    public string ClassName { get; set; }
    public string ObjectId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    //public string Username { get; set; } // Added
    //public string Email { get; set; } // Added
    public string SessionToken { get; set; } // Added

    public IDictionary<string, object> ServerData { get; set; } = new Dictionary<string, object>();

    public object this[string key] => ServerData.ContainsKey(key) ? ServerData[key] : null;

    public bool ContainsKey(string key)
    {
        return ServerData.ContainsKey(key);
    }

    public void Apply(IDictionary<string, IParseFieldOperation> operationSet)
    {
        foreach (var pair in operationSet)
        {
            try
            {
                ServerData.TryGetValue(pair.Key, out var oldValue);
                var newValue = pair.Value.Apply(oldValue, pair.Key);
                if (newValue != ParseDeleteOperation.Token)
                    ServerData[pair.Key] = newValue;
                else
                    ServerData.Remove(pair.Key);
            }
            catch
            {
                // Log and skip incompatible field updates
                Debug.WriteLine($"Skipped incompatible operation for key: {pair.Key}");
            }
        }
    }

    public void Apply(IObjectState other)
    {
        IsNew = other.IsNew;

        if (other.ObjectId != null)
            ObjectId = other.ObjectId;
        if (other.UpdatedAt != null)
            UpdatedAt = other.UpdatedAt;
        if (other.CreatedAt != null)
            CreatedAt = other.CreatedAt;

        foreach (var pair in other)
        {
            try
            {
                ServerData[pair.Key] = pair.Value;
            }
            catch
            {
                // Log and skip incompatible fields
                Debug.WriteLine($"Skipped incompatible field: {pair.Key}");
            }
        }
    }
    public IObjectState MutatedClone(Action<MutableObjectState> func)
    {
        var clone = MutableClone();
        try
        {
            // Apply the mutation function to the clone
            func(clone);
        }
        catch (Exception ex)
        {
            // Log the failure and continue
            Debug.WriteLine($"Skipped incompatible mutation during clone: {ex.Message}");
        }
        return clone;
    }
    protected virtual MutableObjectState MutableClone()
    {
        return new MutableObjectState
        {
            IsNew = IsNew,
            ClassName = ClassName,
            ObjectId = ObjectId,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            //Username= Username,
            //Email = Email,
            //EmailVerified = EmailVerified,
            //SessionToken = SessionToken,
            
            ServerData = ServerData.ToDictionary(entry => entry.Key, entry => entry.Value)
        };
    }

    IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
    {
        return ServerData.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return ServerData.GetEnumerator();
    }

    public static MutableObjectState Decode(object data, IServiceHub serviceHub)
    {
        if (data is IDictionary<string, object> dictionary)
        {
            try
            {
                var state = new MutableObjectState
                {
                    ClassName = dictionary.ContainsKey("className") ? dictionary["className"]?.ToString() : null,
                    ObjectId = dictionary.ContainsKey("objectId") ? dictionary["objectId"]?.ToString() : null,
                    CreatedAt = dictionary.ContainsKey("createdAt") ? DecodeDateTime(dictionary["createdAt"]) : null,
                    UpdatedAt = dictionary.ContainsKey("updatedAt") ? DecodeDateTime(dictionary["updatedAt"]) : null,
                    IsNew = dictionary.ContainsKey("isNew") && Convert.ToBoolean(dictionary["isNew"]),
                    //EmailVerified = dictionary.ContainsKey("emailVerified") && Convert.ToBoolean(dictionary["emailVerified"]),
                    //Username = dictionary.ContainsKey("username") ? dictionary["username"]?.ToString() : null,
                    //Email = dictionary.ContainsKey("email") ? dictionary["email"]?.ToString() : null,
                    //SessionToken = dictionary.ContainsKey("sessionToken") ? dictionary["sessionToken"]?.ToString() : null,
                    ServerData = dictionary
                        .Where(pair => IsValidField(pair.Key, pair.Value))
                        .ToDictionary(pair => pair.Key, pair => pair.Value)
                };

                return state;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to decode MutableObjectState: {ex.Message}");
                return null; // Graceful failure
            }
        }

        Debug.WriteLine("Data is not a compatible object for decoding.");
        return null;
    }

    private static DateTime? DecodeDateTime(object value)
    {
        try
        {
            return value is DateTime dateTime ? dateTime : DateTime.Parse(value.ToString());
        }
        catch
        {
            Debug.WriteLine($"Failed to decode DateTime value: {value}");
            return null; // Graceful fallback
        }
    }

    private static bool IsValidField(string key, object value)
    {
        // Add any validation logic for fields if needed
        return !string.IsNullOrEmpty(key); // Example: Ignore null/empty keys
    }
}
