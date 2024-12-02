using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Installations;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure.Utilities;

namespace Parse.Platform.Installations;
internal class ParseCurrentInstallationController : IParseCurrentInstallationController
{
    private static readonly string ParseInstallationKey = nameof(CurrentInstallation);
    private readonly object Mutex = new object();
    private readonly TaskQueue TaskQueue = new TaskQueue();

    private readonly IParseInstallationController InstallationController;
    private readonly ICacheController StorageController;
    private readonly IParseInstallationCoder InstallationCoder;
    private readonly IParseObjectClassController ClassController;

    private ParseInstallation CurrentInstallationValue { get; set; }

    internal ParseInstallation CurrentInstallation
    {
        get
        {
            lock (Mutex)
            {
                return CurrentInstallationValue;
            }
        }
        set
        {
            lock (Mutex)
            {
                CurrentInstallationValue = value;
            }
        }
    }

    public ParseCurrentInstallationController(
        IParseInstallationController installationIdController,
        ICacheController storageController,
        IParseInstallationCoder installationCoder,
        IParseObjectClassController classController)
    {
        InstallationController = installationIdController;
        StorageController = storageController;
        InstallationCoder = installationCoder;
        ClassController = classController;
    }

    public async Task SetAsync(ParseInstallation installation, CancellationToken cancellationToken)
    {
        // Update the current installation in memory and disk asynchronously
        await TaskQueue.Enqueue<Task>(async (toAwait) =>
        {
            var storage = await StorageController.LoadAsync().ConfigureAwait(false);
            if (installation != null)
            {
                await storage.AddAsync(ParseInstallationKey, JsonUtilities.Encode(InstallationCoder.Encode(installation))).ConfigureAwait(false);
            }
            else
            {
                await storage.RemoveAsync(ParseInstallationKey).ConfigureAwait(false);
            }
            CurrentInstallation = installation;
        }, cancellationToken).ConfigureAwait(false);
    }


    public async Task<ParseInstallation> GetAsync(IServiceHub serviceHub, CancellationToken cancellationToken = default)
    {
        // Check if the installation is already cached
        var cachedCurrent = CurrentInstallation;
        if (cachedCurrent != null)
        {
            return cachedCurrent;
        }

        var storage = await StorageController.LoadAsync().ConfigureAwait(false);
        if (storage.TryGetValue(ParseInstallationKey, out object temp) && temp is string installationDataString)
        {
            var installationData = JsonUtilities.Parse(installationDataString) as IDictionary<string, object>;
            var installation = InstallationCoder.Decode(installationData, serviceHub);
            CurrentInstallation = installation;
            return installation;
        }
        else
        {
            var installation = ClassController.CreateObject<ParseInstallation>(serviceHub);
            var installationId = await InstallationController.GetAsync().ConfigureAwait(false);
            installation.SetIfDifferent("installationId", installationId.ToString());
            CurrentInstallation = installation;
            return installation;
        }
    }

    public async Task<bool> ExistsAsync(CancellationToken cancellationToken)
    {
        // Check if the current installation exists in memory or storage
        if (CurrentInstallation != null)
        {
            return true;
        }

        var storage = await StorageController.LoadAsync().ConfigureAwait(false);
        return storage.ContainsKey(ParseInstallationKey);
    }

    public bool IsCurrent(ParseInstallation installation)
    {
        return CurrentInstallation == installation;
    }

    public void ClearFromMemory()
    {
        CurrentInstallation = null;
    }

    public async Task ClearFromDiskAsync()
    {
        ClearFromMemory();
        var storage = await StorageController.LoadAsync().ConfigureAwait(false);
        await storage.RemoveAsync(ParseInstallationKey).ConfigureAwait(false);
    }
}