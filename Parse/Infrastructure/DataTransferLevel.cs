// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using Parse.Abstractions.Infrastructure;

namespace Parse.Infrastructure
{
    /// <summary>
    /// Represents upload progress.
    /// </summary>
    public class DataTransferLevel : EventArgs, IDataTransferLevel
    {
        /// <summary>
        /// Gets the progress (a number between 0.0 and 1.0) of an upload or download.
        /// </summary>
        public double Amount { get; set; }
    }
}
