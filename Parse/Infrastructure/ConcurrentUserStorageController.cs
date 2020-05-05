using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using static Parse.Resources;

namespace Parse.Infrastructure
{
    public class VirtualCacheController : ICacheController
    {
        class VirtualDataStore : Dictionary<string, object>, IDataCache<string, object>
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

        VirtualDataStore Cache { get; } = new VirtualDataStore { };

        public void Clear() => Cache.Clear();

        public FileInfo GetRelativeFile(string path) => throw new NotSupportedException(ConcurrentUserStorageControllerFileOperationNotSupportedMessage);

        public Task<IDataCache<string, object>> LoadAsync() => Task.FromResult<IDataCache<string, object>>(Cache);

        public Task<IDataCache<string, object>> SaveAsync(IDictionary<string, object> contents)
        {
            foreach (KeyValuePair<string, object> pair in contents)
            {
                ((IDictionary<string, object>) Cache).Add(pair);
            }

            return Task.FromResult<IDataCache<string, object>>(Cache);
        }

        public Task TransferAsync(string originFilePath, string targetFilePath) => Task.FromException(new NotSupportedException(ConcurrentUserStorageControllerFileOperationNotSupportedMessage));
    }
}
