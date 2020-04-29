// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System.Collections.Generic;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Control;

namespace Parse.Infrastructure.Control
{
    /// <summary>
    /// An operation where a field is deleted from the object.
    /// </summary>
    public class ParseDeleteOperation : IParseFieldOperation
    {
        internal static object Token { get; } = new object { };

        public static ParseDeleteOperation Instance { get; } = new ParseDeleteOperation { };

        private ParseDeleteOperation() { }

        public object Encode(IServiceHub serviceHub) => new Dictionary<string, object> { ["__op"] = "Delete" };

        public IParseFieldOperation MergeWithPrevious(IParseFieldOperation previous) => this;

        public object Apply(object oldValue, string key) => Token;
    }
}
