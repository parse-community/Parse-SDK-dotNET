using Parse.Abstractions.Infrastructure;

namespace Parse.Infrastructure
{
    public class MetadataController : IMetadataController
    {
        /// <summary>
        /// Information about your app.
        /// </summary>
        public IHostManifestData HostManifestData { get; set; }

        /// <summary>
        /// Information about the environment the library is operating in.
        /// </summary>
        public IEnvironmentData EnvironmentData { get; set; }
    }
}
