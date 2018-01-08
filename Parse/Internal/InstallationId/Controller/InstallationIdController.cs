// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using Parse.Common.Internal;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Parse.Core.Internal {
  public class InstallationIdController : IInstallationIdController {
    private const string InstallationIdKey = "InstallationId";
    private readonly object mutex = new object();
    private Guid? installationId;

    private readonly IStorageController storageController;
    public InstallationIdController(IStorageController storageController) {
      this.storageController = storageController;
    }

    public Task SetAsync(Guid? installationId) {
      lock (mutex) {
        Task saveTask;

        if (installationId == null) {
          saveTask = storageController
            .LoadAsync()
            .OnSuccess(storage => storage.Result.RemoveAsync(InstallationIdKey))
            .Unwrap();
        } else {
          saveTask = storageController
            .LoadAsync()
            .OnSuccess(storage => storage.Result.AddAsync(InstallationIdKey, installationId.ToString()))
            .Unwrap();
        }
        this.installationId = installationId;
        return saveTask;
      }
    }

    public Task<Guid?> GetAsync() {
      lock (mutex) {
        if (installationId != null) {
          return Task.FromResult(installationId);
        }
      }

      return storageController
        .LoadAsync()
        .OnSuccess<IStorageDictionary<string, object>, Task<Guid?>>(s => {
          object id;
          s.Result.TryGetValue(InstallationIdKey, out id);
          try {
            lock (mutex) {
              installationId = new Guid((string)id);
              return Task.FromResult(installationId);
            }
          } catch (Exception) {
            var newInstallationId = Guid.NewGuid();
            return SetAsync(newInstallationId).OnSuccess<Guid?>(_ => newInstallationId);
          }
        })
        .Unwrap();
    }

    public Task ClearAsync() {
      return SetAsync(null);
    }
  }
}
