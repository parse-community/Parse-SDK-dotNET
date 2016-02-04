// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;

namespace Parse.Common.Internal {
  /// <summary>
  /// Represents an object that can be converted into JSON.
  /// </summary>
  public interface IJsonConvertible {
    /// <summary>
    /// Converts the object to a data structure that can be converted to JSON.
    /// </summary>
    /// <returns>An object to be JSONified.</returns>
    IDictionary<string, object> ToJSON();
  }
}
