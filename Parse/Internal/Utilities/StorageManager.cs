using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace Parse.Internal.Utilities
{
    /// <summary>
    /// A collection of utility methods and properties for writing to the app-specific persistent storage folder.
    /// </summary>
    internal static class StorageManager
    {
        static StorageManager() => Directory.CreateDirectory(PersistentStoragePath);

        /// <summary>
        /// The path to a persistent user-specific storage location specific to the final client assembly of the Parse library.
        /// </summary>
        public static string PersistentStoragePath { get; } = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create), Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationName, Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationVersion));

        /// <summary>
        /// Asynchronously writes the provided little-endian 16-bit character string <paramref name="content"/> to the file wrapped by the provided <see cref="FileInfo"/> instance.
        /// </summary>
        /// <param name="file">The <see cref="FileInfo"/> instance wrapping the target file that is to be written to</param>
        /// <param name="content">The little-endian 16-bit Unicode character string (UTF-16) that is to be written to the <paramref name="file"/></param>
        /// <returns>A task that completes once the write operation to the <paramref name="file"/> completes</returns>
        public static async Task WriteToAsync(this FileInfo file, string content)
        {
            using (FileStream stream = new FileStream(Path.GetFullPath(file.FullName), FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.SequentialScan | FileOptions.Asynchronous))
                await stream.WriteAsync(Encoding.Unicode.GetBytes(content), 0, content.Length);
        }

        /// <summary>
        /// Asynchronously read all of the little-endian 16-bit character units (UTF-16) contained within the file wrapped by the provided <see cref="FileInfo"/> instance.
        /// </summary>
        /// <param name="file">The <see cref="FileInfo"/> instance wrapping the target file that string content is to be read from</param>
        /// <returns>A task that should contain the little-endian 16-bit character string (UTF-16) extracted from the <paramref name="file"/> if the read completes successfully</returns>
        public static async Task<string> ReadAllTextAsync(this FileInfo file)
        {
            using (StreamReader reader = file.OpenText())
                return await reader.ReadToEndAsync();
        }

        /// <summary>
        /// Get or Create a file with it's path based in the <see cref="PersistentStoragePath"/>, named and relatively positioned according to the <paramref name="relativePath"/> parameter.
        /// </summary>
        /// <param name="relativePath">The relative path from the <see cref="PersistentStoragePath"/>, including the name, of the target persistent storage file</param>
        /// <returns>A file wrapper for the target file</returns>
        public static FileInfo GetPersistentStorageFileWrapperAsync(string relativePath)
        {
            FileInfo file = new FileInfo(Path.GetFullPath(Path.Combine(PersistentStoragePath, relativePath)));
            if (!file.Exists) using (file.Create()) ; // Hopefully the JIT doesn't no-op this. The behaviour of the "using" clause should dictate how the stream is closed, to make sure it happens properly.

            return file;
        }

        private static FileInfo CreatePersistentStorageFileAsync(string path)
        {
            new FileStream(path = Path.GetFullPath(Path.Combine(PersistentStoragePath, path)), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan).Dispose();
            return new FileInfo(path);
        }
    }
}
