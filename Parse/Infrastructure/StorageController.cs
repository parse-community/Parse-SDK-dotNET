using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Infrastructure.Utilities;
using static Parse.Resources;

namespace Parse.Infrastructure
{
    public class ConcurrentUserStorageController : IStorageController
    {
        class VirtualStorageDictionary : Dictionary<string, object>, IStorageDictionary<string, object>
        {
            public Task AddAsync(string key, object value)
            {
                Add(key, value);
                return Task.CompletedTask;
            }

            public Task RemoveAsync(string key)
            {
                Remove(key);
                return Task.CompletedTask;
            }
        }

        VirtualStorageDictionary Storage { get; } = new VirtualStorageDictionary { };

        public void Clear() => Storage.Clear();

        public FileInfo GetWrapperForRelativePersistentStorageFilePath(string path) => throw new NotSupportedException(ConcurrentUserStorageControllerFileOperationNotSupportedMessage);

        public Task<IStorageDictionary<string, object>> LoadAsync() => Task.FromResult<IStorageDictionary<string, object>>(Storage);

        public Task<IStorageDictionary<string, object>> SaveAsync(IDictionary<string, object> contents)
        {
            foreach (KeyValuePair<string, object> pair in contents)
                ((IDictionary<string, object>) Storage).Add(pair);

            return Task.FromResult<IStorageDictionary<string, object>>(Storage);
        }

        public Task TransferAsync(string originFilePath, string targetFilePath) => Task.FromException(new NotSupportedException(ConcurrentUserStorageControllerFileOperationNotSupportedMessage));
    }

    /// <summary>
    /// Implements `IStorageController` for PCL targets, based off of PCLStorage.
    /// </summary>
    public class StorageController : IStorageController
    {
        class StorageDictionary : IStorageDictionary<string, object>
        {
            public StorageDictionary(FileInfo file) => File = file;

            internal Task SaveAsync() => Lock(() => File.WriteContentAsync(JsonUtilities.Encode(Storage)));

            internal Task LoadAsync() => File.ReadAllTextAsync().ContinueWith(task =>
            {
                lock (Mutex)
                    try
                    {
                        Storage = JsonUtilities.Parse(task.Result) as Dictionary<string, object>;
                    }
                    catch
                    {
                        Storage = new Dictionary<string, object> { };
                    }
            });

            // TODO: Check if the call to ToDictionary is necessary here considering contents is IDictionary<string object>.

            internal void Update(IDictionary<string, object> contents) => Lock(() => Storage = contents.ToDictionary(element => element.Key, element => element.Value));

            public Task AddAsync(string key, object value)
            {
                lock (Mutex)
                {
                    Storage[key] = value;
                    return SaveAsync();
                }
            }

            public Task RemoveAsync(string key)
            {
                lock (Mutex)
                {
                    Storage.Remove(key);
                    return SaveAsync();
                }
            }

            public void Add(string key, object value) => throw new NotSupportedException(StorageDictionarySynchronousMutationNotSupportedMessage);

            public bool Remove(string key) => throw new NotSupportedException(StorageDictionarySynchronousMutationNotSupportedMessage);

            public void Add(KeyValuePair<string, object> item) => throw new NotSupportedException(StorageDictionarySynchronousMutationNotSupportedMessage);

            public bool Remove(KeyValuePair<string, object> item) => throw new NotSupportedException(StorageDictionarySynchronousMutationNotSupportedMessage);

            public bool ContainsKey(string key) => Lock(() => Storage.ContainsKey(key));

            public bool TryGetValue(string key, out object value)
            {
                lock (Mutex)
                    return (Result: Storage.TryGetValue(key, out object found), value = found).Result;
            }

            public void Clear() => Lock(() => Storage.Clear());

            public bool Contains(KeyValuePair<string, object> item) => Lock(() => Elements.Contains(item));

            public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => Lock(() => Elements.CopyTo(array, arrayIndex));

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => Storage.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => Storage.GetEnumerator();

            public FileInfo File { get; set; }

            public object Mutex { get; set; } = new object { };

            // ALTNAME: Operate

            TResult Lock<TResult>(Func<TResult> operation)
            {
                lock (Mutex)
                    return operation.Invoke();
            }

            void Lock(Action operation)
            {
                lock (Mutex)
                    operation.Invoke();
            }

            ICollection<KeyValuePair<string, object>> Elements => Storage as ICollection<KeyValuePair<string, object>>;

            Dictionary<string, object> Storage { get; set; } = new Dictionary<string, object> { };

            public ICollection<string> Keys => Storage.Keys;

            public ICollection<object> Values => Storage.Values;

            public int Count => Storage.Count;

            public bool IsReadOnly => Elements.IsReadOnly;

            public object this[string key]
            {
                get => Storage[key];
                set => throw new NotSupportedException(StorageDictionarySynchronousMutationNotSupportedMessage);
            }
        }

        FileInfo File { get; }
        StorageDictionary Storage { get; set; }
        TaskQueue Queue { get; } = new TaskQueue { };

        /// <summary>
        /// Creates a Parse storage controller and attempts to extract a previously created settings storage file from the persistent storage location.
        /// </summary>
        public StorageController() => Storage = new StorageDictionary(File = PersistentStorageFileWrapper);

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
            // Check if storage dictionary is already created from the controllers file (create if not)
            Storage ??= new StorageDictionary(File);

            // Load storage dictionary content async and return the resulting dictionary type
            return Queue.Enqueue(toAwait => toAwait.ContinueWith(_ => Storage.LoadAsync().OnSuccess(__ => Storage as IStorageDictionary<string, object>)).Unwrap(), CancellationToken.None);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contents"></param>
        /// <returns></returns>
        public Task<IStorageDictionary<string, object>> SaveAsync(IDictionary<string, object> contents) => Queue.Enqueue(toAwait => toAwait.ContinueWith(_ =>
        {
            (Storage ??= new StorageDictionary(File)).Update(contents);
            return Storage.SaveAsync().OnSuccess(__ => Storage as IStorageDictionary<string, object>);
        }).Unwrap());

        // TODO: Attach the following method to AppDomain.CurrentDomain.ProcessExit if that actually ever made sense for anything except randomly generated file names, otherwise attach the delegate when it is known the file name is a randomly generated string.

        public void Clear()
        {
            if (new FileInfo(FallbackPersistentStorageFilePath) is { Exists: true } file)
                file.Delete();
        }

        /// <summary>
        /// The relative path from the <see cref="Environment.SpecialFolder.LocalApplicationData"/> on the device an to application-specific persistent storage folder.
        /// </summary>
        public string RelativeStorageFilePath { get; set; }

        /// <summary>
        /// The path to a persistent user-specific storage location specific to the final client assembly of the Parse library.
        /// </summary>
        public string PersistentStorageFilePath => Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), RelativeStorageFilePath ?? FallbackPersistentStorageFilePath));

        /// <summary>
        /// Gets the calculated persistent storage file fallback path for this app execution.
        /// </summary>
        public string FallbackPersistentStorageFilePath => StoredFallbackPersistentStorageFilePath ??= IdentifierBasedCacheLocationConfiguration.Fallback.GetRelativeStorageFilePath(new MutableServiceHub { StorageController = this });

        string StoredFallbackPersistentStorageFilePath { get; set; }

        /// <summary>
        /// Gets or creates the file pointed to by <see cref="PersistentStorageFilePath"/> and returns it's wrapper as a <see cref="FileInfo"/> instance.
        /// </summary>
        public FileInfo PersistentStorageFileWrapper
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
        public FileInfo GetWrapperForRelativePersistentStorageFilePath(string path)
        {
            Directory.CreateDirectory((path = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path))).Substring(0, path.LastIndexOf(Path.DirectorySeparatorChar)));
            return new FileInfo(path);
        }

        /// <summary>
        /// Transfers a file from <paramref name="originFilePath"/> to <paramref name="targetFilePath"/>.
        /// </summary>
        /// <param name="originFilePath"></param>
        /// <param name="targetFilePath"></param>
        /// <returns>A task that completes once the file move operation form <paramref name="originFilePath"/> to <paramref name="targetFilePath"/> completes.</returns>
        public async Task TransferAsync(string originFilePath, string targetFilePath)
        {
            if (!String.IsNullOrWhiteSpace(originFilePath) && !String.IsNullOrWhiteSpace(targetFilePath) && new FileInfo(originFilePath) is { Exists: true } originFile && new FileInfo(targetFilePath) is { } targetFile)
            {
                using StreamWriter writer = new StreamWriter(targetFile.OpenWrite(), Encoding.Unicode);
                using StreamReader reader = new StreamReader(originFile.OpenRead(), Encoding.Unicode);

                await writer.WriteAsync(await reader.ReadToEndAsync());
            }
        }
    }
}
