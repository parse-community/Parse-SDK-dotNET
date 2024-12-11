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
    public class CacheController : IDiskFileCacheController
    {
        private class FileBackedCache : IDataCache<string, object>
        {
            private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
            private Dictionary<string, object> Storage = new Dictionary<string, object>();

            public FileBackedCache(FileInfo file) => File = file;

            public FileInfo File { get; set; }

            public ICollection<string> Keys
            {
                get
                {
                    using var semLock = SemaphoreLock.Create(_semaphore);
                    return Storage.Keys.ToArray();
                }
            }

            public ICollection<object> Values
            {
                get
                {
                    using var semLock = SemaphoreLock.Create(_semaphore);
                    return Storage.Values.ToArray();
                }
            }

            public int Count
            {
                get
                {
                    using var semLock = SemaphoreLock.Create(_semaphore);
                    return Storage.Count;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    using var semLock = SemaphoreLock.Create(_semaphore);
                    return ((ICollection<KeyValuePair<string, object>>) Storage).IsReadOnly;
                }
            }

            public object this[string key]
            {
                get
                {
                    using var semLock = SemaphoreLock.Create(_semaphore);
                    if (Storage.TryGetValue(key, out var val))
                        return val;
                    throw new KeyNotFoundException(key);
                }
                set => throw new NotSupportedException(FileBackedCacheSynchronousMutationNotSupportedMessage);
            }

            public async Task LoadAsync()
            {
                using var semLock = await SemaphoreLock.CreateAsync(_semaphore).ConfigureAwait(false);
                try
                {
                    string fileContent = await File.ReadAllTextAsync().ConfigureAwait(false);
                    Storage = JsonUtilities.Parse(fileContent) as Dictionary<string, object> ?? new Dictionary<string, object>();
                }
                catch (IOException ioEx)
                {
                    Console.WriteLine($"IO error while loading cache: {ioEx.Message}");
                    Storage = new Dictionary<string, object>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error while loading cache: {ex.Message}");
                    Storage = new Dictionary<string, object>();
                }
            }

            public async Task SaveAsync()
            {
                using var semLock = await SemaphoreLock.CreateAsync(_semaphore).ConfigureAwait(false);
                try
                {
                    var content = JsonUtilities.Encode(Storage);
                    await File.WriteContentAsync(content).ConfigureAwait(false);
                }
                catch (IOException ioEx)
                {
                    Console.WriteLine($"IO error while saving cache: {ioEx.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected error while saving cache: {ex.Message}");
                }
            }


            internal void Update(IDictionary<string, object> contents)
            {
                using var semLock = SemaphoreLock.Create(_semaphore);
                Storage = contents.ToDictionary(e => e.Key, e => e.Value);
            }

            public async Task AddAsync(string key, object value)
            {
                using var semLock = await SemaphoreLock.CreateAsync(_semaphore).ConfigureAwait(false);
                Storage[key] = value;
                await SaveAsync().ConfigureAwait(false);
            }

            public async Task RemoveAsync(string key)
            {
                using var semLock = await SemaphoreLock.CreateAsync(_semaphore).ConfigureAwait(false);
                Storage.Remove(key);
                await SaveAsync().ConfigureAwait(false);
            }

            // Unsupported synchronous modifications
            public void Add(string key, object value) => throw new NotSupportedException(FileBackedCacheSynchronousMutationNotSupportedMessage);
            public bool Remove(string key) => throw new NotSupportedException(FileBackedCacheSynchronousMutationNotSupportedMessage);
            public void Add(KeyValuePair<string, object> item) => throw new NotSupportedException(FileBackedCacheSynchronousMutationNotSupportedMessage);
            public bool Remove(KeyValuePair<string, object> item) => throw new NotSupportedException(FileBackedCacheSynchronousMutationNotSupportedMessage);

            public bool ContainsKey(string key)
            {
                using var semLock = SemaphoreLock.Create(_semaphore);
                return Storage.ContainsKey(key);
            }

            public bool TryGetValue(string key, out object value)
            {
                using var semLock = SemaphoreLock.Create(_semaphore);
                return Storage.TryGetValue(key, out value);
            }

            public void Clear()
            {
                using var semLock = SemaphoreLock.Create(_semaphore);
                Storage.Clear();
            }

            public bool Contains(KeyValuePair<string, object> item)
            {
                using var semLock = SemaphoreLock.Create(_semaphore);
                return ((ICollection<KeyValuePair<string, object>>) Storage).Contains(item);
            }

            public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
            {
                using var semLock = SemaphoreLock.Create(_semaphore);
                ((ICollection<KeyValuePair<string, object>>) Storage).CopyTo(array, arrayIndex);
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                using var semLock = SemaphoreLock.Create(_semaphore);
                return Storage.ToList().GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                using var semLock = SemaphoreLock.Create(_semaphore);
                return Storage.ToList().GetEnumerator();
            }
        }

        private readonly SemaphoreSlim _cacheSemaphore = new SemaphoreSlim(1, 1);

        FileInfo File { get; set; }
        FileBackedCache Cache { get; set; }
        TaskQueue Queue { get; } = new TaskQueue();

        public CacheController() { }
        public CacheController(FileInfo file) => EnsureCacheExists(file);

        FileBackedCache EnsureCacheExists(FileInfo file = default)
        {
            return Cache ??= new FileBackedCache(file ?? (File ??= PersistentCacheFile));
        }

        public async Task<IDataCache<string, object>> LoadAsync()
        {
            EnsureCacheExists();
            return await Queue.Enqueue(async toAwait =>
            {
                await toAwait.ConfigureAwait(false);
                await Cache.LoadAsync().ConfigureAwait(false);
                return (IDataCache<string, object>) Cache;
            }, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task<IDataCache<string, object>> SaveAsync(IDictionary<string, object> contents)
        {
            EnsureCacheExists();
            return await Queue.Enqueue(async toAwait =>
            {
                await toAwait.ConfigureAwait(false);

                using (await SemaphoreLock.CreateAsync(_cacheSemaphore).ConfigureAwait(false))
                {
                    Cache.Update(contents);
                    await Cache.SaveAsync().ConfigureAwait(false);
                }

                return (IDataCache<string, object>) Cache;
            }, CancellationToken.None).ConfigureAwait(false);
        }


        public void RefreshPaths()
        {
            Cache = new FileBackedCache(File = PersistentCacheFile);
        }

        public void Clear()
        {
            var file = new FileInfo(FallbackRelativeCacheFilePath);
            if (file.Exists)
                file.Delete();
        }

        public string RelativeCacheFilePath { get; set; }

        public string AbsoluteCacheFilePath
        {
            get => StoredAbsoluteCacheFilePath ??
                   Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                                 RelativeCacheFilePath ?? FallbackRelativeCacheFilePath));
            set => StoredAbsoluteCacheFilePath = value;
        }

        string StoredAbsoluteCacheFilePath { get; set; }

        public string FallbackRelativeCacheFilePath
            => StoredFallbackRelativeCacheFilePath ??=
               IdentifierBasedRelativeCacheLocationGenerator.Fallback.GetRelativeCacheFilePath(new MutableServiceHub { CacheController = this });

        string StoredFallbackRelativeCacheFilePath { get; set; }

        public FileInfo PersistentCacheFile
        {
            get
            {
                var dir = Path.GetDirectoryName(AbsoluteCacheFilePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                var file = new FileInfo(AbsoluteCacheFilePath);
                if (!file.Exists)
                    using (file.Create())
                    { }
                return file;
            }
        }

        public FileInfo GetRelativeFile(string path)
        {
            path = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), path));
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return new FileInfo(path);
        }

        public async Task TransferAsync(string originFilePath, string targetFilePath)
        {
            if (string.IsNullOrWhiteSpace(originFilePath) || string.IsNullOrWhiteSpace(targetFilePath))
                return;

            var originFile = new FileInfo(originFilePath);
            if (!originFile.Exists)
                return;
            var targetFile = new FileInfo(targetFilePath);

            using var reader = new StreamReader(originFile.OpenRead(), Encoding.Unicode);
            var content = await reader.ReadToEndAsync().ConfigureAwait(false);

            using var writer = new StreamWriter(targetFile.OpenWrite(), Encoding.Unicode);
            await writer.WriteAsync(content).ConfigureAwait(false);
        }

        internal static class SemaphoreLock
        {
            public static async Task<IDisposable> CreateAsync(SemaphoreSlim semaphore)
            {
                await semaphore.WaitAsync().ConfigureAwait(false);
                return new Releaser(semaphore);
            }

            public static IDisposable Create(SemaphoreSlim semaphore)
            {
                semaphore.Wait();
                return new Releaser(semaphore);
            }

            private sealed class Releaser : IDisposable
            {
                private readonly SemaphoreSlim _sem;
                public Releaser(SemaphoreSlim sem) => _sem = sem;
                public void Dispose() => _sem.Release();
            }
        }
    }
}
