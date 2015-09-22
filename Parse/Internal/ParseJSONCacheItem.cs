// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

namespace Parse.Internal {
  class ParseJSONCacheItem {
    private readonly string comparisonString;

    public ParseJSONCacheItem(object obj) {
      try {
        comparisonString = Json.Encode(
          PointerOrLocalIdEncoder.Instance.Encode(obj));
      } catch {
        comparisonString = "";
        // We can't serialize certain things, like new objects, new files,
        // etc., so just allow them to be re-saved more aggressively.
      }
    }

    public override bool Equals(object obj) {
      var other = (ParseJSONCacheItem)obj;
      return comparisonString.Equals(other.comparisonString);
    }

    public override int GetHashCode() {
      return comparisonString.GetHashCode();
    }
  }
}
