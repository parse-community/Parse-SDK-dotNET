using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using Parse.Internal.Utilities;

namespace Parse.Common.Internal
{
    /// <summary>
    /// Implements `IStorageController` for PCL targets, based off of PCLStorage.
    /// </summary>
    public class StorageController : IStorageController
    {
        private class StorageDictionary : IStorageDictionary<string, object>
        {
            private object mutex;
            private Dictionary<string, object> dictionary;
            private FileInfo file;

            public StorageDictionary(FileInfo file)
            {
                this.file = file;

                mutex = new object();
                dictionary = new Dictionary<string, object>();
            }

            internal Task SaveAsync()
            {
                string json;
                lock (mutex)
                    json = Json.Encode(dictionary);

                return file.WriteToAsync(json);
            }

            internal Task LoadAsync()
            {
                return file.ReadAllTextAsync().ContinueWith(t =>
                {
                    string text = t.Result;
                    Dictionary<string, object> result = null;
                    try
                    {
                        result = Json.Parse(text) as Dictionary<string, object>;
                    }
                    catch (Exception)
                    {
                        // Do nothing, JSON error. Probaby was empty string.
                    }

                    lock (mutex)
                    {
                        dictionary = result ?? new Dictionary<string, object>();
                    }
                });
            }

            internal void Update(IDictionary<string, object> contents)
            {
                lock (mutex)
                {
                    dictionary = contents.ToDictionary(p => p.Key, p => p.Value);
                }
            }

            public Task AddAsync(string key, object value)
            {
                lock (mutex)
                {
                    dictionary[key] = value;
                }
                return SaveAsync();
            }

            public Task RemoveAsync(string key)
            {
                lock (mutex)
                {
                    dictionary.Remove(key);
                }
                return SaveAsync();
            }

            public bool ContainsKey(string key)
            {
                lock (mutex)
                {
                    return dictionary.ContainsKey(key);
                }
            }

            public IEnumerable<string> Keys
            {
                get { lock (mutex) { return dictionary.Keys; } }
            }

            public bool TryGetValue(string key, out object value)
            {
                lock (mutex)
                {
                    return dictionary.TryGetValue(key, out value);
                }
            }

            public IEnumerable<object> Values
            {
                get { lock (mutex) { return dictionary.Values; } }
            }

            public object this[string key]
            {
                get { lock (mutex) { return dictionary[key]; } }
            }

            public int Count
            {
                get { lock (mutex) { return dictionary.Count; } }
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                lock (mutex)
                {
                    return dictionary.GetEnumerator();
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                lock (mutex)
                {
                    return dictionary.GetEnumerator();
                }
            }
        }

        FileInfo File { get; }
        StorageDictionary Storage { get; set; }
        TaskQueue Queue { get; } = new TaskQueue { };

        /// <summary>
        /// Creates a Parse storage controller and attempts to extract a previously created settings storage file from the persistent storage location.
        /// </summary>
        public StorageController() => Storage = new StorageDictionary(File = StorageManager.PersistentStorageFileWrapper);

        /// <summary>
        /// Creates a Parse storage controller with the provided <paramref name="file"/> wrapper.
        /// </summary>
        /// <param name="file">The file wrapper that the storage controller instance should target</param>
        public StorageController(FileInfo file) => File = file;

        /// <summary>
        /// Loads a settings dictionary from the file wrapped by <see cref="File"/>.
        /// </summary>
        /// <returns>A storage dictionary containing the deserialized content of the storage file targeted by the <see cref="StorageController"/> instance</returns>
        public Task<IStorageDictionary<string, object>> LoadAsync()
        {
            // check if storage dictionary is already created from the controllers file (create if not)
            if (Storage == null)
                Storage = new StorageDictionary(File);
            // load storage dictionary content async and return the resulting dictionary type
            return Queue.Enqueue(toAwait => toAwait.ContinueWith(_ => Storage.LoadAsync().OnSuccess(__ => Storage as IStorageDictionary<string, object>)).Unwrap(), CancellationToken.None);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contents"></param>
        /// <returns></returns>
        public Task<IStorageDictionary<string, object>> SaveAsync(IDictionary<string, object> contents) => Queue.Enqueue(toAwait => toAwait.ContinueWith(_ =>
        {
            (Storage ?? (Storage = new StorageDictionary(File))).Update(contents);
            return Storage.SaveAsync().OnSuccess(__ => Storage as IStorageDictionary<string, object>);
        }).Unwrap());
    }
}
