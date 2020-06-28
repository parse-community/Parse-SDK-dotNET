// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Infrastructure.Data;
using Parse.Infrastructure.Utilities;

namespace Parse.Infrastructure.Control
{
    public class ParseRemoveOperation : IParseFieldOperation
    {
        ReadOnlyCollection<object> Data { get; }

        public ParseRemoveOperation(IEnumerable<object> objects) => Data = new ReadOnlyCollection<object>(objects.Distinct().ToList());

        public object Encode(IServiceHub serviceHub) => new Dictionary<string, object>
        {
            ["__op"] = "Remove",
            ["objects"] = PointerOrLocalIdEncoder.Instance.Encode(Data, serviceHub)
        };

        public IParseFieldOperation MergeWithPrevious(IParseFieldOperation previous) => previous switch
        {
            null => this,
            ParseDeleteOperation _ => previous,
            ParseSetOperation setOp => new ParseSetOperation(Apply(Conversion.As<IList<object>>(setOp.Value), default)),
            ParseRemoveOperation oldOp => new ParseRemoveOperation(oldOp.Objects.Concat(Data)),
            _ => throw new InvalidOperationException("Operation is invalid after previous operation.")
        };

        public object Apply(object oldValue, string key) => oldValue is { } ? Conversion.As<IList<object>>(oldValue).Except(Data, ParseFieldOperations.ParseObjectComparer).ToList() : new List<object> { };

        public IEnumerable<object> Objects => Data;
    }
}
