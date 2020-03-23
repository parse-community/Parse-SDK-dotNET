using System;
using System.Collections.Generic;
using System.Text;
using Parse.Abstractions.Library;

namespace Parse.Library
{
    public class MetadataController : IMetadataController
    {
        /// <summary>
        /// The version information of your application environment.
        /// </summary>
        public IHostApplicationVersioningData HostVersioningData { get; set; }
    }
}
