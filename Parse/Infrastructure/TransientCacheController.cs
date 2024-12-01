using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using static Parse.Resources;

namespace Parse.Infrastructure
{
    public class TransientCacheController : ICacheController
    {
        class VirtualCache : Dictionary<string, object>, IDataCache<string, object>
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

        VirtualCache Cache { get; } = new VirtualCache { };

        public void Clear()
        {
            Cache.Clear();
        }

        public FileInfo GetRelativeFile(string path)
        {
            throw new NotSupportedException(TransientCacheControllerDiskFileOperationNotSupportedMessage);
        }

        public Task<IDataCache<string, object>> LoadAsync()
        {
            return Task.FromResult<IDataCache<string, object>>(Cache);
        }

        public Task<IDataCache<string, object>> SaveAsync(IDictionary<string, object> contents)
        {
            foreach (KeyValuePair<string, object> pair in contents)
            {
                ((IDictionary<string, object>) Cache).Add(pair);
            }

            return Task.FromResult<IDataCache<string, object>>(Cache);
        }

        public Task TransferAsync(string originFilePath, string targetFilePath)
        {
            return Task.FromException(new NotSupportedException(TransientCacheControllerDiskFileOperationNotSupportedMessage));
        }
    }
}
