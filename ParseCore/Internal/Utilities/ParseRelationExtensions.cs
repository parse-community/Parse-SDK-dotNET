// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;

namespace Parse.Core.Internal {
  /// <summary>
  /// So here's the deal. We have a lot of internal APIs for ParseObject, ParseUser, etc.
  ///
  /// These cannot be 'internal' anymore if we are fully modularizing things out, because
  /// they are no longer a part of the same library, especially as we create things like
  /// Installation inside push library.
  ///
  /// So this class contains a bunch of extension methods that can live inside another
  /// namespace, which 'wrap' the intenral APIs that already exist.
  /// </summary>
  public static class ParseRelationExtensions {
    public static ParseRelation<T> Create<T>(ParseObject parent, string childKey) where T : ParseObject {
      return new ParseRelation<T>(parent, childKey);
    }

    public static ParseRelation<T> Create<T>(ParseObject parent, string childKey, string targetClassName) where T: ParseObject {
      return new ParseRelation<T>(parent, childKey, targetClassName);
    }

    public static string GetTargetClassName<T>(this ParseRelation<T> relation) where T : ParseObject {
      return relation.TargetClassName;
    }
  }
}
