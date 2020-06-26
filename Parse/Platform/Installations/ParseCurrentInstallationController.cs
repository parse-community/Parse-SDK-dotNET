// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Installations;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure.Utilities;

namespace Parse.Platform.Installations
{
    internal class ParseCurrentInstallationController : IParseCurrentInstallationController
    {
        static string ParseInstallationKey { get; } = nameof(CurrentInstallation);

        object Mutex { get; } = new object { };

        TaskQueue TaskQueue { get; } = new TaskQueue { };

        IParseInstallationController InstallationController { get; }

        ICacheController StorageController { get; }

        IParseInstallationCoder InstallationCoder { get; }

        IParseObjectClassController ClassController { get; }

        public ParseCurrentInstallationController(IParseInstallationController installationIdController, ICacheController storageController, IParseInstallationCoder installationCoder, IParseObjectClassController classController)
        {
            InstallationController = installationIdController;
            StorageController = storageController;
            InstallationCoder = installationCoder;
            ClassController = classController;
        }

        ParseInstallation CurrentInstallationValue { get; set; }

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

        public Task SetAsync(ParseInstallation installation, CancellationToken cancellationToken) => TaskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ =>
        {
            Task saveTask = StorageController.LoadAsync().OnSuccess(storage => installation is { } ? storage.Result.AddAsync(ParseInstallationKey, JsonUtilities.Encode(InstallationCoder.Encode(installation))) : storage.Result.RemoveAsync(ParseInstallationKey)).Unwrap();
            CurrentInstallation = installation;

            return saveTask;
        }).Unwrap(), cancellationToken);

        public Task<ParseInstallation> GetAsync(IServiceHub serviceHub, CancellationToken cancellationToken = default)
        {
            ParseInstallation cachedCurrent;
            cachedCurrent = CurrentInstallation;

            return cachedCurrent is { } ? Task.FromResult(cachedCurrent) : TaskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ => StorageController.LoadAsync().OnSuccess(stroage =>
            {
                Task fetchTask;
                stroage.Result.TryGetValue(ParseInstallationKey, out object temp);
                ParseInstallation installation = default;

                if (temp is string installationDataString)
                {
                    IDictionary<string, object> installationData = JsonUtilities.Parse(installationDataString) as IDictionary<string, object>;
                    installation = InstallationCoder.Decode(installationData, serviceHub);

                    fetchTask = Task.FromResult<object>(null);
                }
                else
                {
                    installation = ClassController.CreateObject<ParseInstallation>(serviceHub);
                    fetchTask = InstallationController.GetAsync().ContinueWith(t => installation.SetIfDifferent("installationId", t.Result.ToString()));
                }

                CurrentInstallation = installation;
                return fetchTask.ContinueWith(task => installation);
            })).Unwrap().Unwrap(), cancellationToken);
        }

        public Task<bool> ExistsAsync(CancellationToken cancellationToken) => CurrentInstallation is { } ? Task.FromResult(true) : TaskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ => StorageController.LoadAsync().OnSuccess(storageTask => storageTask.Result.ContainsKey(ParseInstallationKey))).Unwrap(), cancellationToken);

        public bool IsCurrent(ParseInstallation installation) => CurrentInstallation == installation;

        public void ClearFromMemory() => CurrentInstallation = default;

        public void ClearFromDisk()
        {
            ClearFromMemory();

            TaskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ => StorageController.LoadAsync().OnSuccess(storage => storage.Result.RemoveAsync(ParseInstallationKey))).Unwrap().Unwrap(), CancellationToken.None);
        }
    }
}
