// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;

namespace Parse.Core.Internal {
  /// <summary>
  /// A <see cref="ParseEncoder"/> that throws an exception if it attempts to encode
  /// a <see cref="ParseObject"/>
  /// </summary>
  public class NoObjectsEncoder : ParseEncoder {
    // This class isn't really a Singleton, but since it has no state, it's more efficient to get
    // the default instance.
    private static readonly NoObjectsEncoder instance = new NoObjectsEncoder();
    public static NoObjectsEncoder Instance {
      get {
        return instance;
      }
    }

    protected override IDictionary<string, object> EncodeParseObject(ParseObject value) {
      throw new ArgumentException("ParseObjects not allowed here.");
    }
  }
}
