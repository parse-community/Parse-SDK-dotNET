// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

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

namespace Parse.Platform.Users
{
#warning This class needs to be rewritten (PCuUsC).

    public class ParseCurrentUserController : IParseCurrentUserController
    {
        object Mutex { get; } = new object { };

        TaskQueue TaskQueue { get; } = new TaskQueue { };

        ICacheController StorageController { get; }

        IParseObjectClassController ClassController { get; }

        IParseDataDecoder Decoder { get; }

        public ParseCurrentUserController(ICacheController storageController, IParseObjectClassController classController, IParseDataDecoder decoder) => (StorageController, ClassController, Decoder) = (storageController, classController, decoder);

        ParseUser currentUser;
        public ParseUser CurrentUser
        {
            get
            {
                lock (Mutex)
                    return currentUser;
            }
            set
            {
                lock (Mutex)
                    currentUser = value;
            }
        }

        public Task SetAsync(ParseUser user, CancellationToken cancellationToken) => TaskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ =>
        {
            Task saveTask = default;

            if (user is null)
                saveTask = StorageController.LoadAsync().OnSuccess(task => task.Result.RemoveAsync(nameof(CurrentUser))).Unwrap();
            else
            {
                // TODO (hallucinogen): we need to use ParseCurrentCoder instead of this janky encoding

                IDictionary<string, object> data = user.ServerDataToJSONObjectForSerialization();
                data["objectId"] = user.ObjectId;

                if (user.CreatedAt != null)
                    data["createdAt"] = user.CreatedAt.Value.ToString(ParseClient.DateFormatStrings.First(), CultureInfo.InvariantCulture);
                if (user.UpdatedAt != null)
                    data["updatedAt"] = user.UpdatedAt.Value.ToString(ParseClient.DateFormatStrings.First(), CultureInfo.InvariantCulture);

                saveTask = StorageController.LoadAsync().OnSuccess(task => task.Result.AddAsync(nameof(CurrentUser), JsonUtilities.Encode(data))).Unwrap();
            }

            CurrentUser = user;
            return saveTask;
        }).Unwrap(), cancellationToken);

        public Task<ParseUser> GetAsync(IServiceHub serviceHub, CancellationToken cancellationToken = default)
        {
            ParseUser cachedCurrent;

            lock (Mutex)
                cachedCurrent = CurrentUser;

            return cachedCurrent is { } ? Task.FromResult(cachedCurrent) : TaskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ => StorageController.LoadAsync().OnSuccess(task =>
            {
                task.Result.TryGetValue(nameof(CurrentUser), out object data);
                ParseUser user = default;

                if (data is string { } serialization)
                    user = ClassController.GenerateObjectFromState<ParseUser>(ParseObjectCoder.Instance.Decode(JsonUtilities.Parse(serialization) as IDictionary<string, object>, Decoder, serviceHub), "_User", serviceHub);

                return CurrentUser = user;
            })).Unwrap(), cancellationToken);
        }

        public Task<bool> ExistsAsync(CancellationToken cancellationToken) => CurrentUser is { } ? Task.FromResult(true) : TaskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ => StorageController.LoadAsync().OnSuccess(t => t.Result.ContainsKey(nameof(CurrentUser)))).Unwrap(), cancellationToken);

        public bool IsCurrent(ParseUser user)
        {
            lock (Mutex)
                return CurrentUser == user;
        }

        public void ClearFromMemory() => CurrentUser = default;

        public void ClearFromDisk()
        {
            lock (Mutex)
            {
                ClearFromMemory();

                TaskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ => StorageController.LoadAsync().OnSuccess(t => t.Result.RemoveAsync(nameof(CurrentUser)))).Unwrap().Unwrap(), CancellationToken.None);
            }
        }

        public Task<string> GetCurrentSessionTokenAsync(IServiceHub serviceHub, CancellationToken cancellationToken = default) => GetAsync(serviceHub, cancellationToken).OnSuccess(task => task.Result?.SessionToken);

        public Task LogOutAsync(IServiceHub serviceHub, CancellationToken cancellationToken = default) => TaskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ => GetAsync(serviceHub, cancellationToken)).Unwrap().OnSuccess(task => ClearFromDisk()), cancellationToken);
    }
}
