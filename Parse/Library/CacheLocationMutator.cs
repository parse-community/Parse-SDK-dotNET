using System;
using System.Collections.Generic;
using System.Text;
using Parse.Abstractions.Library;
using Parse.Abstractions.Storage;
using Parse.Common.Internal;

namespace Parse.Library
{
    /// <summary>
    /// An <see cref="IServiceHubMutator"/> for the relative storagecache location.
    /// </summary>
    public class CacheLocationMutator : IServiceHubMutator
    {
        ICacheLocationConfiguration CacheLocationConfiguration { get; set; }

        public bool Valid => CacheLocationConfiguration is { };

        public void Mutate(ref IMutableServiceHub target, in IServiceHub referenceHub) => target.StorageController = (target as IServiceHub).StorageController switch
        {
            null => new StorageController { RelativeStorageFilePath = CacheLocationConfiguration.GetRelativeStorageFilePath(referenceHub) },
            StorageController { } controller => (Controller: controller, controller.RelativeStorageFilePath = CacheLocationConfiguration.GetRelativeStorageFilePath(referenceHub)).Controller,
            { } controller => controller
        };
    }
}
