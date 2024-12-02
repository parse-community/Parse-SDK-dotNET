using System;
using System.Collections.Generic;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Abstractions.Platform.Objects;
using Parse.Platform.Objects;

namespace Parse.Infrastructure.Data
{
    // TODO: (richardross) refactor entire parse coder interfaces.

    public class ParseObjectCoder
    {
        public static ParseObjectCoder Instance { get; } = new ParseObjectCoder { };

        // Prevent default constructor.

        ParseObjectCoder() { }

        public IDictionary<string, object> Encode<T>(T state, IDictionary<string, IParseFieldOperation> operations, ParseDataEncoder encoder, IServiceHub serviceHub) where T : IObjectState
        {
            Dictionary<string, object> result = new Dictionary<string, object> { };
            foreach (KeyValuePair<string, IParseFieldOperation> pair in operations)
            {
                // Serialize the data
                IParseFieldOperation operation = pair.Value;

                result[pair.Key] = encoder.Encode(operation, serviceHub);
            }

            return result;
        }

        public IObjectState Decode(IDictionary<string, object> data, IParseDataDecoder decoder, IServiceHub serviceHub)
        {
            IDictionary<string, object> serverData = new Dictionary<string, object> { }, mutableData = new Dictionary<string, object>(data);

            string objectId = Extract(mutableData, "objectId", (obj) => obj as string);
            string email = Extract(mutableData, "email", (obj) => obj as string);
            string username = Extract(mutableData, "username", (obj) => obj as string);
            string sessionToken = Extract(mutableData, "sessionToken", (obj) => obj as string);
            string error = Extract(mutableData, "error", (obj) => obj as string);
            int code = Extract(mutableData, "code", obj => Convert.ToInt32(obj));
            bool emailVerified = Extract(mutableData, "emailVerified", (obj) => (bool) obj);    
            DateTime? createdAt = Extract<DateTime?>(mutableData, "createdAt", (obj) => ParseDataDecoder.ParseDate(obj as string)), updatedAt = Extract<DateTime?>(mutableData, "updatedAt", (obj) => ParseDataDecoder.ParseDate(obj as string));



            if (!mutableData.ContainsKey("username"))
            {
                serverData["username"] = username;
            }
            if (!mutableData.ContainsKey("email"))
            {
                serverData["email"] = email;
            }
            if (!mutableData.ContainsKey("sessionToken"))
            {
                serverData["sessionToken"] = sessionToken;
            }
            if (!mutableData.ContainsKey("error"))
            {
                serverData["error"] = error;
            }
            if (!mutableData.ContainsKey("code"))
            {
                serverData["code"] = code;
            }            
            if (!mutableData.ContainsKey("emailVerified"))
            {
                serverData["emailVerified"] = emailVerified;
            }
            if (!mutableData.ContainsKey("ACL"))
            {
                var e = Extract(mutableData, "ACL", (obj) =>
                {
                    return new ParseACL(obj as IDictionary<string, object>);
                });
            }

            if (createdAt != null && updatedAt == null)
            {
                updatedAt = createdAt;
            }

            // Bring in the new server data.

            foreach (KeyValuePair<string, object> pair in mutableData)
            {
                if (pair.Key == "__type" || pair.Key == "className")
                {
                    continue;
                }

                serverData[pair.Key] = decoder.Decode(pair.Value, serviceHub);
            }

            return new MutableObjectState
            {
                ObjectId = objectId,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt,
                ServerData = serverData,
                //Email = email,
                //Username = username,
                //EmailVerified = emailVerified,
                SessionToken = sessionToken
            };
        }

        T Extract<T>(IDictionary<string, object> data, string key, Func<object, T> action)
        {
            T result = default;

            if (data.ContainsKey(key))
            {
                result = action(data[key]);
                data.Remove(key);
            }

            return result;
        }
    }
}
