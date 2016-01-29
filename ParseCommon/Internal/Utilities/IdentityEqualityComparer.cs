// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Parse.Common.Internal
{
    /// <summary>
    /// An equality comparer that uses the object identity (i.e. ReferenceEquals)
    /// rather than .Equals, allowing identity to be used for checking equality in
    /// ISets and IDictionaries.
    /// </summary>
    public class IdentityEqualityComparer<T> : IEqualityComparer<T>
    {
        public bool Equals(T x, T y)
        {
            return object.ReferenceEquals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
