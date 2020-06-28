// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure.Control;
using Parse.Infrastructure.Utilities;

namespace Parse.Infrastructure.Data
{
    public class ParseDataDecoder : IParseDataDecoder
    {
        // Prevent default constructor.

        IParseObjectClassController ClassController { get; }

        public ParseDataDecoder(IParseObjectClassController classController) => ClassController = classController;

        static string[] Types { get; } = { "Date", "Bytes", "Pointer", "File", "GeoPoint", "Object", "Relation" };

        public object Decode(object data, IServiceHub serviceHub) => data switch
        {
            null => default,
            IDictionary<string, object> { } dictionary when dictionary.ContainsKey("__op") => ParseFieldOperations.Decode(dictionary),
            IDictionary<string, object> { } dictionary when dictionary.TryGetValue("__type", out object type) && Types.Contains(type) => type switch
            {
                "Date" => ParseDate(dictionary["iso"] as string),
                "Bytes" => Convert.FromBase64String(dictionary["base64"] as string),
                "Pointer" => DecodePointer(dictionary["className"] as string, dictionary["objectId"] as string, serviceHub),
                "File" => new ParseFile(dictionary["name"] as string, new Uri(dictionary["url"] as string)),
                "GeoPoint" => new ParseGeoPoint(Conversion.To<double>(dictionary["latitude"]), Conversion.To<double>(dictionary["longitude"])),
                "Object" => ClassController.GenerateObjectFromState<ParseObject>(ParseObjectCoder.Instance.Decode(dictionary, this, serviceHub), dictionary["className"] as string, serviceHub),
                "Relation" => serviceHub.CreateRelation(null, null, dictionary["className"] as string)
            },
            IDictionary<string, object> { } dictionary => dictionary.ToDictionary(pair => pair.Key, pair => Decode(pair.Value, serviceHub)),
            IList<object> { } list => list.Select(item => Decode(item, serviceHub)).ToList(),
            _ => data
        };

        protected virtual object DecodePointer(string className, string objectId, IServiceHub serviceHub) => ClassController.CreateObjectWithoutData(className, objectId, serviceHub);

        // TODO(hallucinogen): Figure out if we should be more flexible with the date formats we accept.

        public static DateTime ParseDate(string input) => DateTime.ParseExact(input, ParseClient.DateFormatStrings, CultureInfo.InvariantCulture, DateTimeStyles.None);
    }
}
