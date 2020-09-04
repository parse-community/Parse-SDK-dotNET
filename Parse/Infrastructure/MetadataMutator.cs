using System.Collections.Generic;

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
        /// Sets the <paramref name="mutableHub"/>'s <see cref="IServiceHub.MetadataController"/> to the <see cref="MetadataMutator"/> instance.
        /// </summary>
        /// <param name="mutableHub">The <see cref="IMutableServiceHub"/> to compose the information onto.</param>
        /// <param name="consumableHub">The <see cref="IServiceHub"/> to use if a default service instance is required.</param>
        /// <param name="futureMutators">The mutators that will be executed after this one.</param>
        public void Mutate(ref IMutableServiceHub mutableHub, in IServiceHub consumableHub, Stack<IServiceHubMutator> futureMutators) => mutableHub.MetadataController = this;
    }
}
