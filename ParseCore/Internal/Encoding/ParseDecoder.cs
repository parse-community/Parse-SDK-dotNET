// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using Parse.Utilities;

namespace Parse.Core.Internal {
  public class ParseDecoder {
    // This class isn't really a Singleton, but since it has no state, it's more efficient to get
    // the default instance.
    private static readonly ParseDecoder instance = new ParseDecoder();
    public static ParseDecoder Instance {
      get {
        return instance;
      }
    }

    // Prevent default constructor.
    private ParseDecoder() { }

    public object Decode(object data) {
      if (data == null) {
        return null;
      }

      var dict = data as IDictionary<string, object>;
      if (dict != null) {
        if (dict.ContainsKey("__op")) {
          return ParseFieldOperations.Decode(dict);
        }

        object type;
        dict.TryGetValue("__type", out type);
        var typeString = type as string;

        if (typeString == null) {
          var newDict = new Dictionary<string, object>();
          foreach (var pair in dict) {
            newDict[pair.Key] = Decode(pair.Value);
          }
          return newDict;
        }

        if (typeString == "Date") {
          return ParseDate(dict["iso"] as string);
        }

        if (typeString == "Bytes") {
          return Convert.FromBase64String(dict["base64"] as string);
        }

        if (typeString == "Pointer") {
          return DecodePointer(dict["className"] as string, dict["objectId"] as string);
        }

        if (typeString == "File") {
          return new ParseFile(dict["name"] as string, new Uri(dict["url"] as string));
        }

        if (typeString == "GeoPoint") {
          return new ParseGeoPoint(Conversion.To<double>(dict["latitude"]),
              Conversion.To<double>(dict["longitude"]));
        }

        if (typeString == "Object") {
          var state = ParseObjectCoder.Instance.Decode(dict, this);
          return ParseObject.FromState<ParseObject>(state, dict["className"] as string);
        }

        if (typeString == "Relation") {
          return ParseRelationBase.CreateRelation(null, null, dict["className"] as string);
        }

        var converted = new Dictionary<string, object>();
        foreach (var pair in dict) {
          converted[pair.Key] = Decode(pair.Value);
        }
        return converted;
      }

      var list = data as IList<object>;
      if (list != null) {
        return (from item in list
                select Decode(item)).ToList();
      }

      return data;
    }

    protected virtual object DecodePointer(string className, string objectId) {
      return ParseObject.CreateWithoutData(className, objectId);
    }

    public static DateTime ParseDate(string input) {
      // TODO(hallucinogen): Figure out if we should be more flexible with the date formats
      // we accept.
      return DateTime.ParseExact(input,
        ParseClient.DateFormatStrings,
        CultureInfo.InvariantCulture,
        DateTimeStyles.None);
    }
  }
}
