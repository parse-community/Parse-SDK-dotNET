// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;

namespace Parse.Core.Internal {
  /// <summary>
  /// A <see cref="ParseEncoder"/> that encode <see cref="ParseObject"/> as pointers. If the object
  /// does not have an <see cref="ParseObject.ObjectId"/>, uses a local id.
  /// </summary>
  public class PointerOrLocalIdEncoder : ParseEncoder {
    // This class isn't really a Singleton, but since it has no state, it's more efficient to get
    // the default instance.
    private static readonly PointerOrLocalIdEncoder instance = new PointerOrLocalIdEncoder();
    public static PointerOrLocalIdEncoder Instance {
      get {
        return instance;
      }
    }

    protected override IDictionary<string, object> EncodeParseObject(ParseObject value) {
      if (value.ObjectId == null) {
        // TODO (hallucinogen): handle local id. For now we throw.
        throw new ArgumentException("Cannot create a pointer to an object without an objectId");
      }

      return new Dictionary<string, object> {
        {"__type", "Pointer"},
        {"className", value.ClassName},
        {"objectId", value.ObjectId}
      };
    }
  }
}
