using Parse.Abstractions.Infrastructure;

namespace Parse.Infrastructure
{
    /// <summary>
    /// An <see cref="IServiceHubMutator"/> for the relative cache file location. This should be used if the relative cache file location is not created correctly by the SDK, such as platforms on which it is not possible to gather metadata about the client assembly, or ones on which <see cref="System.Environment.SpecialFolder.LocalApplicationData"/> is inaccsessible.
    /// </summary>
    public class RelativeCacheLocationMutator : IServiceHubMutator
    {
        /// <summary>
        /// An <see cref="IRelativeCacheLocationGenerator"/> implementation instance which creates a path that should be used as the <see cref="System.Environment.SpecialFolder.LocalApplicationData"/>-relative cache location.
        /// </summary>
        public IRelativeCacheLocationGenerator RelativeCacheLocationGenerator { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool Valid => RelativeCacheLocationGenerator is { };

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="target"><inheritdoc/></param>
        /// <param name="referenceHub"><inheritdoc/></param>
        public void Mutate(ref IMutableServiceHub target, in IServiceHub referenceHub)
        {
            target.CacheController = (target as IServiceHub).CacheController switch
            {
                null => new CacheController { RelativeCacheFilePath = RelativeCacheLocationGenerator.GetRelativeCacheFilePath(referenceHub) },
                IDiskFileCacheController { } controller => (Controller: controller, controller.RelativeCacheFilePath = RelativeCacheLocationGenerator.GetRelativeCacheFilePath(referenceHub)).Controller,
                { } controller => controller
            };
        }
    }
}
