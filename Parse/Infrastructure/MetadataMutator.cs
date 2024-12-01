using Parse.Abstractions.Infrastructure;

namespace Parse.Infrastructure
{
    /// <summary>
    /// An <see cref="IServiceHubMutator"/> for setting metadata information manually.
    /// </summary>
    public class MetadataMutator : MetadataController, IServiceHubMutator
    {
        /// <summary>
        /// A value representing whether or not all of the required metadata information has been provided.
        /// </summary>
        public bool Valid => this is { EnvironmentData: { OSVersion: { }, Platform: { }, TimeZone: { } }, HostManifestData: { Identifier: { }, Name: { }, ShortVersion: { }, Version: { } } };

        /// <summary>
        /// Sets the <paramref name="target"/> to the <see cref="MetadataMutator"/> instance.
        /// </summary>
        /// <param name="target">The <see cref="IMutableServiceHub"/> to compose the information onto.</param>
        /// <param name="referenceHub">Thhe <see cref="IServiceHub"/> to use if a default service instance is required.</param>
        public void Mutate(ref IMutableServiceHub target, in IServiceHub referenceHub)
        {
            target.MetadataController = this;
        }
    }
}
