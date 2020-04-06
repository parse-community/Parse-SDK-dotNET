// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Parse.Common.Internal;
using Parse.Utilities;

namespace Parse.Core.Internal
{
    /// <summary>
    /// A <c>ParseEncoder</c> can be used to transform objects such as <see cref="ParseObject"/> into JSON
    /// data structures.
    /// </summary>
    /// <seealso cref="ParseDataDecoder"/>
    public abstract class ParseDataEncoder
    {
        public static bool Validate(object value) => value is null || value.GetType().IsPrimitive || value is string || value is ParseObject || value is ParseACL || value is ParseFile || value is ParseGeoPoint || value is ParseRelationBase || value is DateTime || value is byte[] || Conversion.As<IDictionary<string, object>>(value) is { } || Conversion.As<IList<object>>(value) is { };

        // If this object has a special encoding, encode it and return the encoded object. Otherwise, just return the original object.

        public object Encode(object value) => value switch
        {
            DateTime { } date => new Dictionary<string, object>
            {
                ["iso"] = date.ToString(ParseClient.DateFormatStrings.First(), CultureInfo.InvariantCulture),
                ["__type"] = "Date"
            },
            byte[] { } bytes => new Dictionary<string, object>
            {
                ["__type"] = "Bytes",
                ["base64"] = Convert.ToBase64String(bytes)
            },
            ParseObject { } entity => EncodeObject(entity),
            IJsonConvertible { } jsonConvertible => jsonConvertible.ConvertToJSON(),
            { } when Conversion.As<IDictionary<string, object>>(value) is { } dictionary => dictionary.ToDictionary(pair => pair.Key, pair => Encode(pair.Value)),
            { } when Conversion.As<IList<object>>(value) is { } list => EncodeList(list),

            // TODO (hallucinogen): convert IParseFieldOperation to IJsonConvertible

            IParseFieldOperation { } fieldOperation => fieldOperation.Encode(),
            _ => value
        };

        protected abstract IDictionary<string, object> EncodeObject(ParseObject value);

        object EncodeList(IList<object> list)
        {
            List<object> encoded = new List<object> { };

            // We need to explicitly cast `list` to `List<object>` rather than `IList<object>` because IL2CPP is stricter than the usual Unity AOT compiler pipeline.

            if (ParseClient.IL2CPPCompiled && list.GetType().IsArray)
            {
                list = new List<object>(list);
            }

            foreach (object item in list)
            {
                if (!Validate(item))
                {
                    throw new ArgumentException("Invalid type for value in an array");
                }

                encoded.Add(Encode(item));
            }

            return encoded;
        }
    }
}
