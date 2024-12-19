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

namespace Parse.Infrastructure;

public class CacheController : IDiskFileCacheController
{
    private class FileBackedCache : IDataCache<string, object>
    {
        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();
        private Dictionary<string, object> Storage = new Dictionary<string, object>();

        public FileBackedCache(FileInfo file) => File = file;

        public FileInfo File { get; set; }

        public ICollection<string> Keys
        {
            get
            {
                _rwLock.EnterReadLock();
                try
                {
                    return Storage.Keys.ToArray();
                }
                finally
                {
                    _rwLock.ExitReadLock();
                }
            }
        }

        public ICollection<object> Values
        {
            get
            {
                _rwLock.EnterReadLock();
                try
                {
                    return Storage.Values.ToArray();
                }
                finally
                {
                    _rwLock.ExitReadLock();
                }
            }
        }


        public int Count
        {
            get
            {
                _rwLock.EnterReadLock();
                try
                {
                    return Storage.Count;
                }
                finally
                {
                    _rwLock.ExitReadLock();
                }
            }
        }

        public bool IsReadOnly
        {
            get
            {
                _rwLock.EnterReadLock();
                try
                {
                    return ((ICollection<KeyValuePair<string, object>>) Storage).IsReadOnly;
                }
                finally
                {
                    _rwLock.ExitReadLock();
                }
            }
        }

        public object this[string key]
        {
            get
            {
                _rwLock.EnterReadLock();
                try
                {
                    if (Storage.TryGetValue(key, out var val))
                        return val;
                    throw new KeyNotFoundException(key);
                }
                finally
                {
                    _rwLock.ExitReadLock();
                }
            }
            set => throw new NotSupportedException(FileBackedCacheSynchronousMutationNotSupportedMessage);
        }

        public async Task LoadAsync()
        {
            try
            {
                string fileContent = await File.ReadAllTextAsync().ConfigureAwait(false);
                var data = JsonUtilities.Parse(fileContent) as Dictionary<string, object>;
                _rwLock.EnterWriteLock();
                try
                {
                    Storage = data ?? new Dictionary<string, object>();
                }
                finally
                {
                    _rwLock.ExitWriteLock();
                }
            }
            catch (IOException ioEx)
            {
                Console.WriteLine($"IO error while loading cache: {ioEx.Message}");
            }
        }

        public async Task SaveAsync()
        {
            Dictionary<string, object> snapshot;
            _rwLock.EnterReadLock();
            try
            {
                snapshot = new Dictionary<string, object>(Storage); // Create a snapshot
            }
            finally
            {
                _rwLock.ExitReadLock();
            }

            try
            {
                var content = JsonUtilities.Encode(snapshot);
                await File.WriteContentAsync(content).ConfigureAwait(false);
            }
            catch (IOException ioEx)
            {
                Console.WriteLine($"IO error while saving cache: {ioEx.Message}");
            }
        }

        public void Update(IDictionary<string, object> contents)
        {
            _rwLock.EnterWriteLock();
            try
            {
                Storage = new Dictionary<string, object>(contents);
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public async Task AddAsync(string key, object value)
        {
            _rwLock.EnterWriteLock();
            try
            {
                Storage[key] = value;
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
            await SaveAsync().ConfigureAwait(false);
        }

        public async Task RemoveAsync(string key)
        {
            _rwLock.EnterWriteLock();
            try
            {
                Storage.Remove(key);
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
            await SaveAsync().ConfigureAwait(false);
        }
    

    // Unsupported synchronous modifications
    public void Add(string key, object value) => throw new NotSupportedException(FileBackedCacheSynchronousMutationNotSupportedMessage);
        public bool Remove(string key) => throw new NotSupportedException(FileBackedCacheSynchronousMutationNotSupportedMessage);
        public void Add(KeyValuePair<string, object> item) => throw new NotSupportedException(FileBackedCacheSynchronousMutationNotSupportedMessage);
        public bool Remove(KeyValuePair<string, object> item) => throw new NotSupportedException(FileBackedCacheSynchronousMutationNotSupportedMessage);

        public bool ContainsKey(string key)
        {
            _rwLock.EnterReadLock();
            try
            {
                return Storage.ContainsKey(key);
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }

        public bool TryGetValue(string key, out object value)
        {
            _rwLock.EnterReadLock();
            try
            {
                return Storage.TryGetValue(key, out value);
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }

        public void Clear()
        {
            _rwLock.EnterWriteLock();
            try
            {
                Storage.Clear();
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            _rwLock.EnterReadLock();
            try
            {
                return ((ICollection<KeyValuePair<string, object>>) Storage).Contains(item);
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            _rwLock.EnterReadLock();
            try
            {
                ((ICollection<KeyValuePair<string, object>>) Storage).CopyTo(array, arrayIndex);
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            _rwLock.EnterReadLock();
            try
            {
                return Storage.ToList().GetEnumerator();
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            _rwLock.EnterReadLock();
            try
            {
                return Storage.ToList().GetEnumerator();
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
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
            _ = semaphore;
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
