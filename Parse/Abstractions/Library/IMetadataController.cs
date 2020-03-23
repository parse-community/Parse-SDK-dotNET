using System.Collections.Generic;
using System.Text;

namespace Parse.Abstractions.Library
{
    /// <summary>
    /// A controller for <see cref="ParseClient"/> metadata. This is provided in a dependency injection container because if a beta feature is activated for a client managing a specific aspect of application operation, then this might need to be reflected in the application versioning information as it is used to determine the data cache location.
    /// </summary>
    /// <remarks>This container could have been implemented as a <see langword="class"/> or <see langword="struct"/>, due to it's simplicity but, more information may be added in the future so it is kept general.</remarks>
    public interface IMetadataController
    {
        /// <summary>
        /// The version information of your application environment.
        /// </summary>
        public IHostApplicationVersioningData HostVersioningData { get; }
    }
}
