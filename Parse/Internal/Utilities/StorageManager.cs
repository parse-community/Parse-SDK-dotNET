using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Parse.Internal.Utilities
{
    /// <summary>
    /// A collection of utility methods and properties for writing to the app-specific persistent storage folder.
    /// </summary>
    internal static class StorageManager
    {
        static StorageManager() => AppDomain.CurrentDomain.ProcessExit += (_, __) => { if (new FileInfo(FallbackPersistentStorageFilePath) is FileInfo file && file.Exists) file.Delete(); };

        /// <summary>
        /// The path to a persistent user-specific storage location specific to the final client assembly of the Parse library.
        /// </summary>
        public static string PersistentStorageFilePath => Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ParseClient.CurrentConfiguration.StorageConfiguration?.RelativeStorageFilePath ?? FallbackPersistentStorageFilePath));

        /// <summary>
        /// Gets the calculated persistent storage file fallback path for this app execution.
        /// </summary>
        public static string FallbackPersistentStorageFilePath { get; } = ParseClient.Configuration.IdentifierBasedStorageConfiguration.Fallback.RelativeStorageFilePath;

        /// <summary>
        /// Asynchronously writes the provided little-endian 16-bit character string <paramref name="content"/> to the file wrapped by the provided <see cref="FileInfo"/> instance.
        /// </summary>
        /// <param name="file">The <see cref="FileInfo"/> instance wrapping the target file that is to be written to</param>
        /// <param name="content">The little-endian 16-bit Unicode character string (UTF-16) that is to be written to the <paramref name="file"/></param>
        /// <returns>A task that completes once the write operation to the <paramref name="file"/> completes</returns>
        public static async Task WriteToAsync(this FileInfo file, string content)
        {
            using (FileStream stream = new FileStream(Path.GetFullPath(file.FullName), FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.SequentialScan | FileOptions.Asynchronous))
            {
                byte[] data = Encoding.Unicode.GetBytes(content);
                await stream.WriteAsync(data, 0, data.Length);
            }
        }

        /// <summary>
        /// Asynchronously read all of the little-endian 16-bit character units (UTF-16) contained within the file wrapped by the provided <see cref="FileInfo"/> instance.
        /// </summary>
        /// <param name="file">The <see cref="FileInfo"/> instance wrapping the target file that string content is to be read from</param>
        /// <returns>A task that should contain the little-endian 16-bit character string (UTF-16) extracted from the <paramref name="file"/> if the read completes successfully</returns>
        public static async Task<string> ReadAllTextAsync(this FileInfo file)
        {
            using (StreamReader reader = new StreamReader(file.OpenRead(), Encoding.Unicode))
                return await reader.ReadToEndAsync();
        }

        /// <summary>
        /// Gets or creates the file pointed to by <see cref="PersistentStorageFilePath"/> and returns it's wrapper as a <see cref="FileInfo"/> instance.
        /// </summary>
        public static FileInfo PersistentStorageFileWrapper
        {
            get
            {
                Directory.CreateDirectory(PersistentStorageFilePath.Substring(0, PersistentStorageFilePath.LastIndexOf(Path.DirectorySeparatorChar)));

                FileInfo file = new FileInfo(PersistentStorageFilePath);
                if (!file.Exists)
                    using (file.Create())
                        ; // Hopefully the JIT doesn't no-op this. The behaviour of the "using" clause should dictate how the stream is closed, to make sure it happens properly.

                return file;
            }
        }

        /// <summary>
        /// Gets the file wrapper for the specified <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The relative path to the target file</param>
        /// <returns>An instance of <see cref="FileInfo"/> wrapping the the <paramref name="path"/> value</returns>
        public static FileInfo GetWrapperForRelativePersistentStorageFilePath(string path)
        {
            path = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path));

            Directory.CreateDirectory(path.Substring(0, path.LastIndexOf(Path.DirectorySeparatorChar)));
            return new FileInfo(path);
        }

        public static async Task TransferAsync(string originFilePath, string targetFilePath)
        {
            if (!String.IsNullOrWhiteSpace(originFilePath) && !String.IsNullOrWhiteSpace(targetFilePath) && new FileInfo(originFilePath) is FileInfo originFile && originFile.Exists && new FileInfo(targetFilePath) is FileInfo targetFile)
                using (StreamWriter writer = new StreamWriter(targetFile.OpenWrite(), Encoding.Unicode))
                using (StreamReader reader = new StreamReader(originFile.OpenRead(), Encoding.Unicode))
                    await writer.WriteAsync(await reader.ReadToEndAsync());
        }
    }
}
