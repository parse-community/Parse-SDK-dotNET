using Parse.Abstractions.Infrastructure;

namespace Parse.Infrastructure
{
    /// <summary>
    /// An <see cref="IServiceHubMutator"/> implementation which changes the <see cref="IServiceHub.CacheController"/>'s <see cref="IDiskFileCacheController.AbsoluteCacheFilePath"/> if available.
    /// </summary>
    public class AbsoluteCacheLocationMutator : IServiceHubMutator
    {
        /// <summary>
        /// A custom absolute cache file path to be set on the active <see cref="IServiceHub.CacheController"/> if it implements <see cref="IDiskFileCacheController"/>.
        /// </summary>
        public string CustomAbsoluteCacheFilePath { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool Valid => CustomAbsoluteCacheFilePath is { };

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="target"><inheritdoc/></param>
        /// <param name="composedHub"><inheritdoc/></param>
        public void Mutate(ref IMutableServiceHub target, in IServiceHub composedHub)
        {
            if ((target as IServiceHub).CacheController is IDiskFileCacheController { } diskFileCacheController)
            {
                diskFileCacheController.AbsoluteCacheFilePath = CustomAbsoluteCacheFilePath;
                diskFileCacheController.RefreshPaths();
            }
        }
    }
}
