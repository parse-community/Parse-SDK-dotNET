using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Parse.Common.Internal {
  /// <summary>
  /// An abstraction for accessing persistent storage in the Parse SDK.
  /// </summary>
  public interface IStorageController {
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
  public interface IStorageDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> {
    int Count { get; }
    TValue this[TKey key] { get; }

    IEnumerable<TKey> Keys { get; }
    IEnumerable<TValue> Values { get; }

    bool ContainsKey(TKey key);
    bool TryGetValue(TKey key, out TValue value);

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