// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;

namespace Parse.Core.Internal
{
    // TODO: (richardross) refactor entire parse coder interfaces.
    public class ParseObjectCoder
    {
        public static ParseObjectCoder Instance { get; } = new ParseObjectCoder();

        // Prevent default constructor.
        private ParseObjectCoder() { }

        public IDictionary<string, object> Encode<T>(T state, IDictionary<string, IParseFieldOperation> operations, ParseEncoder encoder) where T : IObjectState
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (KeyValuePair<string, IParseFieldOperation> pair in operations)
            {
                // Serialize the data
                IParseFieldOperation operation = pair.Value;

                result[pair.Key] = encoder.Encode(operation);
            }

            return result;
        }

        public IObjectState Decode(IDictionary<string, object> data, ParseDecoder decoder)
        {
            IDictionary<string, object> serverData = new Dictionary<string, object>();
            Dictionary<string, object> mutableData = new Dictionary<string, object>(data);
            string objectId = extractFromDictionary(mutableData, "objectId", (obj) => obj as string);
            DateTime? createdAt = extractFromDictionary<DateTime?>(mutableData, "createdAt", (obj) => ParseDecoder.ParseDate(obj as string));
            DateTime? updatedAt = extractFromDictionary<DateTime?>(mutableData, "updatedAt", (obj) => ParseDecoder.ParseDate(obj as string));

            if (mutableData.ContainsKey("ACL"))
            {
                serverData["ACL"] = extractFromDictionary(mutableData, "ACL", (obj) => new ParseACL(obj as IDictionary<string, object>));
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

                object value = pair.Value;
                serverData[pair.Key] = decoder.Decode(value);
            }

            return new MutableObjectState
            {
                ObjectId = objectId,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt,
                ServerData = serverData
            };
        }

        private T extractFromDictionary<T>(IDictionary<string, object> data, string key, Func<object, T> action)
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
