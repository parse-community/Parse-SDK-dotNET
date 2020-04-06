using System;
using System.IO;
using Parse.Abstractions.Library;
using Parse.Abstractions.Management;
using Parse.Abstractions.Storage;

namespace Parse.Storage
{
    /// <summary>
    /// A configuration of the Parse SDK persistent storage location based on an identifier.
    /// </summary>
    public struct IdentifierBasedCacheLocationConfiguration : ICacheLocationConfiguration
    {
        internal static IdentifierBasedCacheLocationConfiguration Fallback { get; } = new IdentifierBasedCacheLocationConfiguration { IsFallback = true };

        /// <summary>
        /// Dictates whether or not this <see cref="ICacheLocationConfiguration"/> instance should act as a fallback for when <see cref="ParseClient"/> has not yet been initialized but the storage path is needed.
        /// </summary>
        internal bool IsFallback { get; set; }

        /// <summary>
        /// The identifier that all Parse SDK cache files should be labelled with.
        /// </summary>
        public string Identifier { get; set; }

        /// <summary>
        /// The corresponding relative path generated by this <see cref="ICacheLocationConfiguration"/>.
        /// </summary>
        /// <remarks>This will cause a .cachefile file extension to be added to the cache file in order to prevent the creation of files with unwanted extensions due to the value of <see cref="Identifier"/> containing periods.</remarks>
        public string GetRelativeStorageFilePath(IServiceHub serviceHub)
        {
            FileInfo file;

            while ((file = serviceHub.StorageController.GetWrapperForRelativePersistentStorageFilePath(GeneratePath())).Exists && IsFallback)
                ;

            return file.FullName;
        }

        /// <summary>
        /// Generates a path for use in the <see cref="GetRelativeStorageFilePath(IServiceHub)"/> method.
        /// </summary>
        /// <returns>A potential path to the cachefile</returns>
        string GeneratePath() => Path.Combine(nameof(Parse), IsFallback ? "_fallback" : "_global", $"{(IsFallback ? new Random { }.Next().ToString() : Identifier)}.cachefile");
    }
}
