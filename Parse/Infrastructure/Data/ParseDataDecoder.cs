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
    private static DateTime? DecodeDateTime(object value)
    {
        try
        {
            // Handle cases where the value is already a DateTime
            if (value is DateTime dateTime)
            {
                return dateTime;
            }

            // Handle string representations of dates
            if (value is string dateString)
            {
                if (DateTime.TryParse(dateString, out var parsedDate))
                {
                    return parsedDate;
                }
            }

            // Handle Unix timestamp (milliseconds since epoch)
            if (value is long unixTimestamp)
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(unixTimestamp).UtcDateTime;
            }

            // Handle Unix timestamp (seconds since epoch)
            if (value is int unixTimestampSeconds)
            {
                return DateTimeOffset.FromUnixTimeSeconds(unixTimestampSeconds).UtcDateTime;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to decode DateTime value: {value}, Error: {ex.Message}");
        }

        // Return null if decoding fails
        return null;
    }

    static string[] Types { get; } = { "Date", "Bytes", "Pointer", "File", "GeoPoint", "Object", "Relation" };
    public object Decode(object data, IServiceHub serviceHub)
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
                };

                return state;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to decode MutableObjectState: {ex.Message}");
                throw; // Let the caller handle decoding errors
            }
        }
        Debug.WriteLine("Data is not a compatible object for decoding. " + data.GetType());

        if (data.GetType() == typeof(string))
        {
            Debug.WriteLine($"Data is not a compatible object for decoding. {data.GetType()} {data}");
        }
        else if (data.GetType() == typeof(Int64))
        {
            Debug.WriteLine($"Data is not a compatible object for decoding. {data.GetType()} Value: {data}");
        }        
        else
        {
            Debug.WriteLine("Data is not a compatible object for decoding. Unknown Type");
        }

        
        return null;
        //throw new InvalidCastException("Input data cannot be cast to IObjectState.");
    }

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

    private object DecodeByType(IDictionary<string, object> dictionary, string type, IServiceHub serviceHub)
    {
        try
        {
            dictionary = NormalizeDictionary(dictionary); // Normalize input dictionary

            switch (type)
            {
                case "Date":
                    if (dictionary.TryGetValue("iso", out var iso))
                    {
                        if (iso is string isoString)
                        {
                            return ParseDate(isoString);
                        }
                        else
                        {
                            Debug.WriteLine($"Unexpected type for 'iso': {iso.GetType()}");
                            throw new ArgumentException($"Invalid type for 'iso' field. Expected string, got {iso.GetType()}.");
                        }
                    }
                    Debug.WriteLine("Missing 'iso' field for Date.");
                    throw new ArgumentException("Invalid or missing 'iso' field for Date.");

                // Handle other cases similarly
                case "Bytes":
                    if (dictionary.TryGetValue("base64", out var base64) && base64 is string base64String)
                    {
                        return Convert.FromBase64String(base64String);
                    }
                    throw new ArgumentException("Invalid or missing 'base64' field for Bytes.");

                case "Pointer":
                    if (dictionary.TryGetValue("className", out var className) && className is string classNameString &&
                        dictionary.TryGetValue("objectId", out var objectId) && objectId is string objectIdString)
                    {
                        return DecodePointer(classNameString, objectIdString, serviceHub);
                    }
                    throw new ArgumentException("Invalid or missing fields for Pointer.");

                case "File":
                    if (dictionary.TryGetValue("name", out var name) && name is string nameString &&
                        dictionary.TryGetValue("url", out var url) && url is string urlString)
                    {
                        return new ParseFile(nameString, new Uri(urlString));
                    }
                    throw new ArgumentException("Invalid or missing fields for File.");

                case "GeoPoint":
                    if (dictionary.TryGetValue("latitude", out var latitude) &&
                        dictionary.TryGetValue("longitude", out var longitude))
                    {
                        return new ParseGeoPoint(
                            Conversion.To<double>(latitude),
                            Conversion.To<double>(longitude)
                        );
                    }
                    throw new ArgumentException("Invalid or missing fields for GeoPoint.");

                case "Object":
                    if (dictionary.TryGetValue("className", out var objectClassName) && objectClassName is string objectClassNameString)
                    {
                        var state = ParseObjectCoder.Instance.Decode(dictionary, this, serviceHub);
                        return ClassController.GenerateObjectFromState<ParseObject>(state, objectClassNameString, serviceHub);
                    }
                    throw new ArgumentException("Invalid or missing fields for Object.");

                case "Relation":
                    if (dictionary.TryGetValue("className", out var relationClassName) && relationClassName is string relationClassNameString)
                    {
                        return serviceHub.CreateRelation(null, null, relationClassNameString);
                    }
                    throw new ArgumentException("Invalid or missing fields for Relation.");

                default:
                    throw new NotSupportedException($"Unsupported __type: {type}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"DecodeByType failed for type '{type}': {ex.Message}");
            throw; // Re-throw to preserve stack trace
        }
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
