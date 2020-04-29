using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Parse.Abstractions.Infrastructure
{
    /// <summary>
    /// An abstraction for accessing persistent storage in the Parse SDK.
    /// </summary>
    public interface IStorageController
    {
        /// <summary>
        /// Cleans up any temporary files and/or directories created during SDK operation.
        /// </summary>
        public void Clear();

        /// <summary>
        /// Gets the file wrapper for the specified <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The relative path to the target file</param>
        /// <returns>An instance of <see cref="FileInfo"/> wrapping the the <paramref name="path"/> value</returns>
        FileInfo GetWrapperForRelativePersistentStorageFilePath(string path);

        /// <summary>
        /// Transfers a file from <paramref name="originFilePath"/> to <paramref name="targetFilePath"/>.
        /// </summary>
        /// <param name="originFilePath"></param>
        /// <param name="targetFilePath"></param>
        /// <returns>A task that completes once the file move operation form <paramref name="originFilePath"/> to <paramref name="targetFilePath"/> completes.</returns>
        Task TransferAsync(string originFilePath, string targetFilePath);

        /// <summary>
        /// Load the contents of this storage controller asynchronously.
        /// </summary>
        /// <returns></returns>
        Task<IStorageDictionary<string, object>> LoadAsync();

        /// <summary>
        /// Overwrites the contents of this storage controller asynchronously.
        /// </summary>
        /// <param name="contents"></param>
        /// <returns></returns>
        Task<IStorageDictionary<string, object>> SaveAsync(IDictionary<string, object> contents);
    }

    /// <summary>
    /// An interface for a dictionary that is persisted to disk asynchronously.
    /// </summary>
    /// <typeparam name="TKey">They key type of the dictionary.</typeparam>
    /// <typeparam name="TValue">The value type of the dictionary.</typeparam>
    public interface IStorageDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        /// <summary>
        /// Adds a key to this dictionary, and saves it asynchronously.
        /// </summary>
        /// <param name="key">The key to insert.</param>
        /// <param name="value">The value to insert.</param>
        /// <returns></returns>
        Task AddAsync(TKey key, TValue value);

        /// <summary>
        /// Removes a key from this dictionary, and saves it asynchronously.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task RemoveAsync(TKey key);
    }
}