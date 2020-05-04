using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using static Parse.Resources;

namespace Parse.Infrastructure
{
    public class ConcurrentUserStorageController : ICacheController
    {
        class VirtualStorageDictionary : Dictionary<string, object>, IDataCache<string, object>
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

        public FileInfo GetRelativeFile(string path) => throw new NotSupportedException(ConcurrentUserStorageControllerFileOperationNotSupportedMessage);

        public Task<IDataCache<string, object>> LoadAsync() => Task.FromResult<IDataCache<string, object>>(Storage);

        public Task<IDataCache<string, object>> SaveAsync(IDictionary<string, object> contents)
        {
            foreach (KeyValuePair<string, object> pair in contents)
                ((IDictionary<string, object>) Storage).Add(pair);

            return Task.FromResult<IDataCache<string, object>>(Storage);
        }

        public Task TransferAsync(string originFilePath, string targetFilePath) => Task.FromException(new NotSupportedException(ConcurrentUserStorageControllerFileOperationNotSupportedMessage));
    }
}
