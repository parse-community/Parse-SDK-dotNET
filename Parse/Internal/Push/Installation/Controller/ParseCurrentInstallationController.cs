// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using Parse.Common.Internal;
using Parse.Core.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Parse.Push.Internal
{
    internal class ParseCurrentInstallationController : IParseCurrentInstallationController
    {
        private const string ParseInstallationKey = "CurrentInstallation";

        private readonly object mutex = new object();
        private readonly TaskQueue taskQueue = new TaskQueue();
        private readonly IInstallationIdController installationIdController;
        private readonly IStorageController storageController;
        private readonly IParseInstallationCoder installationCoder;

        public ParseCurrentInstallationController(IInstallationIdController installationIdController, IStorageController storageController, IParseInstallationCoder installationCoder)
        {
            this.installationIdController = installationIdController;
            this.storageController = storageController;
            this.installationCoder = installationCoder;
        }

        private ParseInstallation currentInstallation;
        internal ParseInstallation CurrentInstallation
        {
            get
            {
                lock (mutex)
                {
                    return currentInstallation;
                }
            }
            set
            {
                lock (mutex)
                {
                    currentInstallation = value;
                }
            }
        }

        public Task SetAsync(ParseInstallation installation, CancellationToken cancellationToken)
        {
            return taskQueue.Enqueue(toAwait =>
            {
                return toAwait.ContinueWith(_ =>
                {
                    Task saveTask = storageController.LoadAsync().OnSuccess(storage =>
                    {
                        if (installation == null)
                        {
                            return storage.Result.RemoveAsync(ParseInstallationKey);
                        }
                        else
                        {
                            var data = installationCoder.Encode(installation);
                            return storage.Result.AddAsync(ParseInstallationKey, Json.Encode(data));
                        }
                    }).Unwrap();

                    CurrentInstallation = installation;
                    return saveTask;
                }).Unwrap();
            }, cancellationToken);
        }

        public Task<ParseInstallation> GetAsync(CancellationToken cancellationToken)
        {
            ParseInstallation cachedCurrent;
            cachedCurrent = CurrentInstallation;

            if (cachedCurrent != null)
            {
                return Task<ParseInstallation>.FromResult(cachedCurrent);
            }

            return taskQueue.Enqueue(toAwait =>
            {
                return toAwait.ContinueWith(_ =>
                {
                    return storageController.LoadAsync().OnSuccess(stroage =>
                    {
                        Task fetchTask;
                        object temp;
                        stroage.Result.TryGetValue(ParseInstallationKey, out temp);
                        var installationDataString = temp as string;
                        ParseInstallation installation = null;
                        if (installationDataString != null)
                        {
                            var installationData = Json.Parse(installationDataString) as IDictionary<string, object>;
                            installation = installationCoder.Decode(installationData);

                            fetchTask = Task.FromResult<object>(null);
                        }
                        else
                        {
                            installation = ParseObject.Create<ParseInstallation>();
                            fetchTask = installationIdController.GetAsync().ContinueWith(t =>
                            {
                                installation.SetIfDifferent("installationId", t.Result.ToString());
                            });
                        }

                        CurrentInstallation = installation;
                        return fetchTask.ContinueWith(t => installation);
                    });
                }).Unwrap().Unwrap();
            }, cancellationToken);
        }

        public Task<bool> ExistsAsync(CancellationToken cancellationToken)
        {
            if (CurrentInstallation != null)
            {
                return Task<bool>.FromResult(true);
            }

            return taskQueue.Enqueue(toAwait =>
            {
                return toAwait.ContinueWith(_ =>
                {
                    return storageController.LoadAsync().OnSuccess(s => s.Result.ContainsKey(ParseInstallationKey));
                }).Unwrap();
            }, cancellationToken);
        }

        public bool IsCurrent(ParseInstallation installation)
        {
            return CurrentInstallation == installation;
        }

        public void ClearFromMemory()
        {
            CurrentInstallation = null;
        }

        public void ClearFromDisk()
        {
            ClearFromMemory();

            taskQueue.Enqueue(toAwait =>
            {
                return toAwait.ContinueWith(_ =>
                {
                    return storageController.LoadAsync().OnSuccess(storage => storage.Result.RemoveAsync(ParseInstallationKey));
                }).Unwrap().Unwrap();
            }, CancellationToken.None);
        }
    }
}
