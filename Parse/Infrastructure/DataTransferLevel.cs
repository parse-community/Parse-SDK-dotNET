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
