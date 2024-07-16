using System.Collections.Generic;

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

        /// <inheritdoc/>
        public bool Valid => CustomAbsoluteCacheFilePath is { };

        /// <inheritdoc/>
        public void Mutate(ref IMutableServiceHub target, in IServiceHub composedHub, Stack<IServiceHubMutator> futureMutators)
        {
            if ((target as IServiceHub).CacheController is IDiskFileCacheController { } diskFileCacheController)
            {
                diskFileCacheController.AbsoluteCacheFilePath = CustomAbsoluteCacheFilePath;
                diskFileCacheController.RefreshPaths();
            }
        }
    }
}
