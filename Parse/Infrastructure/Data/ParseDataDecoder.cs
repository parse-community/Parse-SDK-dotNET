using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure.Control;
using Parse.Infrastructure.Utilities;
using Parse.Platform.Objects;

namespace Parse.Infrastructure.Data;

public class ParseDataDecoder : IParseDataDecoder
{
    // Prevent default constructor.

    IParseObjectClassController ClassController { get; }

    public ParseDataDecoder(IParseObjectClassController classController) => ClassController = classController;
    

    static string[] Types { get; } = { "Date", "Bytes", "Pointer", "File", "GeoPoint", "Object", "Relation" };

    public object Decode(object data, IServiceHub serviceHub)
    {
        try
        {
            // Handle dictionary objects
            if (data is IDictionary<string, object> dictionary)
            {
                return DecodeDictionary(dictionary, serviceHub);
            }

            // Handle list objects
            if (data is IList<object> list)
            {
                return DecodeList(list, serviceHub);
            }

            // Handle primitive types (strings, numbers, etc.)
            if (data is string str)
            {
                return DecodeString(str);
            }

            if (data is long || data is int)
            {
                Debug.WriteLine($"Integer data processed: {data}");
                return data;
            }
            if (data is bool)
            {
                Debug.WriteLine($"Bool data processed: {data}");
                return data;
            }

            // Fallback for unsupported types
            Debug.WriteLine($"Unsupported data type encountered: {data.GetType()}");
            return data;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Decode failed: {ex.Message}");
            return data; // Return raw data on failure
        }
    }

    private object DecodeDictionary(IDictionary<string, object> dictionary, IServiceHub serviceHub)
    {
        // Handle "__op" operations
        if (dictionary.ContainsKey("__op"))
        {
            Debug.WriteLine("Decoding operation field (__op).");
            return ParseFieldOperations.Decode(dictionary);
        }

        // Handle "__type" objects
        if (dictionary.TryGetValue("__type", out var type) && Types.Contains(type.ToString()))
        {
            Debug.WriteLine($"Decoding Parse type object: {type}");
            return DecodeByType(dictionary, type.ToString(), serviceHub);
        }

        // Handle Parse object metadata (e.g., className, objectId)
        if (dictionary.ContainsKey("className"))
        {
            return DecodeObjectState(dictionary);
        }

        // Recursively decode nested dictionaries
        return dictionary.ToDictionary(pair => pair.Key, pair =>
        {
            try
            {
                return Decode(pair.Value, serviceHub);
            }
            catch
            {
                Debug.WriteLine($"Failed to decode nested field: {pair.Key}");
                return pair.Value; // Return raw value if decoding fails
            }
        });
    }

    private object DecodeList(IList<object> list, IServiceHub serviceHub)
    {
        return list.Select(item =>
        {
            try
            {
                return Decode(item, serviceHub);
            }
            catch
            {
                Debug.WriteLine("Failed to decode list item. Returning raw value.");
                return item; // Return raw value on failure
            }
        }).ToList();
    }

    private object DecodeString(string str)
    {
        // Example: Identify session tokens or other meaningful strings
        if (str.StartsWith("r:"))
        {
            Debug.WriteLine($"Valid session token detected: {str}");
            return str;
        }

        Debug.WriteLine($"String data processed: {str}");
        return str;
    }

    private object DecodeObjectState(IDictionary<string, object> dictionary)
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
                ServerData = dictionary
            };

            Debug.WriteLine($"Successfully decoded MutableObjectState for {state.ClassName}, ObjectId: {state.ObjectId}");
            return state;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to decode MutableObjectState: {ex.Message}");
            throw; // Let the caller handle errors
        }
    }

    private object DecodeByType(IDictionary<string, object> dictionary, string type, IServiceHub serviceHub)
    {
        switch (type)
        {
            case "Date":
                return DecodeDateTime(dictionary["iso"]);
            case "Pointer":
                return DecodePointer(dictionary);
            case "GeoPoint":
                return DecodeGeoPoint(dictionary);
            default:
                Debug.WriteLine($"Unsupported Parse type: {type}");
                return dictionary; // Return raw dictionary for unsupported types
        }
    }

    private DateTime DecodeDateTime(object data)
    {
        return DateTime.Parse(data.ToString()); // Assumes ISO-8601 format
    }

    private object DecodePointer(IDictionary<string, object> dictionary)
    {
        return new { ClassName = dictionary["className"], ObjectId = dictionary["objectId"] };
    }

    private object DecodeGeoPoint(IDictionary<string, object> dictionary)
    {
        return new { Latitude = dictionary["latitude"], Longitude = dictionary["longitude"] };
    }



    //static string[] Types { get; } = { "Date", "Bytes", "Pointer", "File", "GeoPoint", "Object", "Relation" };
    //public object Decode(object data, IServiceHub serviceHub)
    //{
    //    if (data is IDictionary<string, object> dictionary)
    //    {
    //        try
    //        {
    //            var state = new MutableObjectState
    //            {
    //                ClassName = dictionary.ContainsKey("className") ? dictionary["className"]?.ToString() : null,
    //                ObjectId = dictionary.ContainsKey("objectId") ? dictionary["objectId"]?.ToString() : null,
    //                CreatedAt = dictionary.ContainsKey("createdAt") ? DecodeDateTime(dictionary["createdAt"]) : null,
    //                UpdatedAt = dictionary.ContainsKey("updatedAt") ? DecodeDateTime(dictionary["updatedAt"]) : null,
    //                IsNew = dictionary.ContainsKey("isNew") && Convert.ToBoolean(dictionary["isNew"]),
    //                //EmailVerified = dictionary.ContainsKey("emailVerified") && Convert.ToBoolean(dictionary["emailVerified"]),
    //                //Username = dictionary.ContainsKey("username") ? dictionary["username"]?.ToString() : null,
    //                //Email = dictionary.ContainsKey("email") ? dictionary["email"]?.ToString() : null,
    //                SessionToken = dictionary.ContainsKey("sessionToken") ? dictionary["sessionToken"]?.ToString() : null,
    //                ServerData = dictionary
    //            };

    //            return state;
    //        }
    //        catch (Exception ex)
    //        {
    //            Debug.WriteLine($"Failed to decode MutableObjectState: {ex.Message}");
    //            throw; // Let the caller handle decoding errors
    //        }
    //    }
    //    Debug.WriteLine("Data is not a compatible object for decoding. " + data.GetType());

    //    if (data.GetType() == typeof(string))
    //    {
    //        Debug.WriteLine($"Data is not a compatible object for decoding. {data.GetType()} {data}");
    //    }
    //    else if (data.GetType() == typeof(Int64))
    //    {
    //        Debug.WriteLine($"Data is not a compatible object for decoding. {data.GetType()} Value: {data}");
    //    }        
    //    else
    //    {
    //        Debug.WriteLine("Data is not a compatible object for decoding. Unknown Type");
    //    }

        
    //    return null;
    //    //throw new InvalidCastException("Input data cannot be cast to IObjectState.");
    //}

    //public object Decode(object data, IServiceHub serviceHub)
    //{
    //    try
    //    {
    //        if (data == null)
    //        {
    //            return default;
    //        }

    //        if (data is IDictionary<string, object> dictionary)
    //        {
    //            // Handle "__op" operations
    //            if (dictionary.ContainsKey("__op"))
    //            {
    //                return ParseFieldOperations.Decode(dictionary);
    //            }

    //            // Handle "__type" objects
    //            if (dictionary.TryGetValue("__type", out var type) && Types.Contains(type))
    //            {
    //                return DecodeByType(dictionary, type.ToString(), serviceHub);
    //            }

    //            // Decode nested dictionary
    //            return dictionary.ToDictionary(pair => pair.Key, pair =>
    //            {
    //                try
    //                {
    //                    return Decode(pair.Value, serviceHub);
    //                }
    //                catch
    //                {
    //                    // Fallback to the original value if decoding fails
    //                    return pair.Value;
    //                }
    //            });
    //        }

    //        // Handle lists
    //        if (data is IList<object> list)
    //        {
    //            return list.Select(item =>
    //            {
    //                try
    //                {
    //                    return Decode(item, serviceHub);
    //                }
    //                catch
    //                {
    //                    // Fallback to the original item if decoding fails
    //                    return item;
    //                }
    //            }).ToList();
    //        }

    //        // Fallback to returning the original data
    //        return data;
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine($"Decode failed: {ex.Message}");
    //        return data; // Fallback to original data if top-level decoding fails
    //    }
    //}

    private IDictionary<string, object> NormalizeDictionary(IDictionary<string, object> input)
    {
        return input.ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    protected virtual object DecodePointer(string className, string objectId, IServiceHub serviceHub)
    {
        return ClassController.CreateObjectWithoutData(className, objectId, serviceHub);
    }

    // TODO(hallucinogen): Figure out if we should be more flexible with the date formats we accept.

    public static DateTime ParseDate(string input)
    {
        return DateTime.ParseExact(input, ParseClient.DateFormatStrings, CultureInfo.InvariantCulture, DateTimeStyles.None);
    }
}
