using System;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Installations;
using Parse.Infrastructure.Utilities;

namespace Parse.Platform.Installations
{
    public class ParseInstallationController : IParseInstallationController
    {
        static string InstallationIdKey { get; } = "InstallationId";

        object Mutex { get; } = new object { };

        Guid? InstallationId { get; set; }

        ICacheController StorageController { get; }

        public ParseInstallationController(ICacheController storageController) => StorageController = storageController;

        public Task SetAsync(Guid? installationId)
        {
            lock (Mutex)
            {
#pragma warning disable CS1030 // #warning directive
#warning Should refactor here if this operates correctly.

                Task saveTask = installationId is { } ? StorageController.LoadAsync().OnSuccess(storage => storage.Result.AddAsync(InstallationIdKey, installationId.ToString())).Unwrap() : StorageController.LoadAsync().OnSuccess(storage => storage.Result.RemoveAsync(InstallationIdKey)).Unwrap();
#pragma warning restore CS1030 // #warning directive

                InstallationId = installationId;
                return saveTask;
            }
        }

        public async Task<Guid?> GetAsync()
        {
            lock (Mutex)
            {
                if (InstallationId != null)
                {
                    return InstallationId;
                }
            }

            // Await the asynchronous storage loading task
            var storageResult = await StorageController.LoadAsync();

            // Try to get the installation ID from the storage result
            if (storageResult.TryGetValue(InstallationIdKey, out object id) && id is string idString && Guid.TryParse(idString, out Guid parsedId))
            {
                lock (Mutex)
                {
                    InstallationId = parsedId; // Cache the parsed ID
                    return InstallationId;
                }
            }

            // If no valid ID is found, generate a new one
            Guid newInstallationId = Guid.NewGuid();
            await SetAsync(newInstallationId); // Save the new ID

            lock (Mutex)
            {
                InstallationId = newInstallationId; // Cache the new ID
                return InstallationId;
            }
        }

        public Task ClearAsync()
        {
            return SetAsync(null);
        }
    }
}
