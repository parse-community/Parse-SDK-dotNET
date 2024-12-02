using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Objects;
using Parse.Abstractions.Platform.Users;
using Parse.Infrastructure.Utilities;
using Parse.Infrastructure.Data;
using System;

namespace Parse.Platform.Users;

#pragma warning disable CS1030 // #warning directive
#warning This class needs to be rewritten (PCuUsC).


public class ParseCurrentUserController : IParseCurrentUserController
{
    private readonly ICacheController StorageController;
    private readonly IParseObjectClassController ClassController;
    private readonly IParseDataDecoder Decoder;

    private readonly TaskQueue TaskQueue = new();
    private ParseUser? currentUser; // Nullable to explicitly handle absence of a user

    public ParseCurrentUserController(ICacheController storageController, IParseObjectClassController classController, IParseDataDecoder decoder)
    {
        StorageController = storageController ?? throw new ArgumentNullException(nameof(storageController));
        ClassController = classController ?? throw new ArgumentNullException(nameof(classController));
        Decoder = decoder ?? throw new ArgumentNullException(nameof(decoder));
    }

    public ParseUser? CurrentUser
    {
        get => currentUser;
        private set => currentUser = value; // Setter is private to ensure controlled modification
    }
    private static string GenerateParseObjectId()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 10)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public async Task SetAsync(ParseUser? user, CancellationToken cancellationToken)
    {
        await TaskQueue.Enqueue<Task<bool>>(async _ =>
        {
            if (user == null)
            {
                var storage = await StorageController.LoadAsync().ConfigureAwait(false);
                await storage.RemoveAsync(nameof(CurrentUser)).ConfigureAwait(false);
            }
            else
            {
                // Use ParseCurrentCoder for encoding if available
                var data = new Dictionary<string, object>
                {
                    ["objectId"] = user.ObjectId ?? GenerateParseObjectId()
                };

                // Additional properties can be added to the dictionary as needed


                if (user.CreatedAt != null)
                    data["createdAt"] = user.CreatedAt.Value.ToString(ParseClient.DateFormatStrings.First(), CultureInfo.InvariantCulture);

                if (user.UpdatedAt != null)
                    data["updatedAt"] = user.UpdatedAt.Value.ToString(ParseClient.DateFormatStrings.First(), CultureInfo.InvariantCulture);

                var storage = await StorageController.LoadAsync().ConfigureAwait(false);
                await storage.AddAsync(nameof(CurrentUser), JsonUtilities.Encode(data)).ConfigureAwait(false);
            }

            CurrentUser = user;
            return true; // Enforce return type as `Task<bool>`
        }, cancellationToken).ConfigureAwait(false);
    }


    public async Task<ParseUser?> GetAsync(IServiceHub serviceHub, CancellationToken cancellationToken = default)
    {
        if (CurrentUser is { ObjectId: { } })
            return CurrentUser;

        return await TaskQueue.Enqueue<Task<ParseUser?>>(async _ =>
        {
            var storage = await StorageController.LoadAsync().ConfigureAwait(false);
            if (storage.TryGetValue(nameof(CurrentUser), out var serializedData) && serializedData is string serialization)
            {
                var state = ParseObjectCoder.Instance.Decode(JsonUtilities.Parse(serialization) as IDictionary<string, object>, Decoder, serviceHub);
                CurrentUser = ClassController.GenerateObjectFromState<ParseUser>(state, "_User", serviceHub);
            }
            else
            {
                CurrentUser = null;
            }

            return CurrentUser; // Explicitly return the current user (or null)
        }, cancellationToken).ConfigureAwait(false);
    }


    public async Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
    {
        return CurrentUser != null || await TaskQueue.Enqueue(async _ =>
        {
            var storage = await StorageController.LoadAsync().ConfigureAwait(false);
            return storage.ContainsKey(nameof(CurrentUser));
        }, cancellationToken).ConfigureAwait(false);
    }

    public bool IsCurrent(ParseUser user) => CurrentUser == user;

    public void ClearFromMemory() => CurrentUser = null;

    public async Task ClearFromDiskAsync()
    {
        ClearFromMemory();
        await TaskQueue.Enqueue(async _ =>
        {
            var storage = await StorageController.LoadAsync().ConfigureAwait(false);
            await storage.RemoveAsync(nameof(CurrentUser)).ConfigureAwait(false);
        }, CancellationToken.None).ConfigureAwait(false);
    }

    public async Task<string?> GetCurrentSessionTokenAsync(IServiceHub serviceHub, CancellationToken cancellationToken = default)
    {
        var user = await GetAsync(serviceHub, cancellationToken).ConfigureAwait(false);
        return user?.SessionToken;
    }

    public async Task LogOutAsync(IServiceHub serviceHub, CancellationToken cancellationToken = default)
    {
        await TaskQueue.Enqueue(async _ =>
        {
            await GetAsync(serviceHub, cancellationToken).ConfigureAwait(false);
            ClearFromDiskAsync();
        }, cancellationToken).ConfigureAwait(false);
    }
}