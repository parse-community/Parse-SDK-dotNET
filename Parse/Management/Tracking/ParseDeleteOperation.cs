// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System.Collections.Generic;

namespace Parse.Core.Internal
{
    /// <summary>
    /// An operation where a field is deleted from the object.
    /// </summary>
    public class ParseDeleteOperation : IParseFieldOperation
    {
        internal static readonly object DeleteToken = new object();
        private static ParseDeleteOperation _Instance = new ParseDeleteOperation();
        public static ParseDeleteOperation Instance => _Instance;

        private ParseDeleteOperation() { }
        public object Encode() => new Dictionary<string, object> {
        {"__op", "Delete"}
      };

        public IParseFieldOperation MergeWithPrevious(IParseFieldOperation previous) => this;

        public object Apply(object oldValue, string key) => DeleteToken;
    }
}
