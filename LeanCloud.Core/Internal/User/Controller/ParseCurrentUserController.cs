// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Common.Internal;

namespace LeanCloud.Core.Internal {
  public class AVCurrentUserController : IAVCurrentUserController {
    private readonly object mutex = new object();
    private readonly TaskQueue taskQueue = new TaskQueue();

    private IStorageController storageController;

    public AVCurrentUserController(IStorageController storageController) {
      this.storageController = storageController;
    }

    private AVUser currentUser;
    public AVUser CurrentUser {
      get {
        lock (mutex) {
          return currentUser;
        }
      }
      set {
        lock (mutex) {
          currentUser = value;
        }
      }
    }

    public Task SetAsync(AVUser user, CancellationToken cancellationToken) {
      return taskQueue.Enqueue(toAwait => {
        return toAwait.ContinueWith(_ => {
          Task saveTask = null;
          if (user == null) {
            saveTask = storageController
              .LoadAsync()
              .OnSuccess(t => t.Result.RemoveAsync("CurrentUser"))
              .Unwrap();
          } else {
            // TODO (hallucinogen): we need to use ParseCurrentCoder instead of this janky encoding
            var data = user.ServerDataToJSONObjectForSerialization();
            data["objectId"] = user.ObjectId;
            if (user.CreatedAt != null) {
              data["createdAt"] = user.CreatedAt.Value.ToString(AVClient.DateFormatStrings.First(),
                CultureInfo.InvariantCulture);
            }
            if (user.UpdatedAt != null) {
              data["updatedAt"] = user.UpdatedAt.Value.ToString(AVClient.DateFormatStrings.First(),
                CultureInfo.InvariantCulture);
            }

            saveTask = storageController
              .LoadAsync()
              .OnSuccess(t => t.Result.AddAsync("CurrentUser", Json.Encode(data)))
              .Unwrap();
          }
          CurrentUser = user;

          return saveTask;
        }).Unwrap();
      }, cancellationToken);
    }

    public Task<AVUser> GetAsync(CancellationToken cancellationToken) {
      AVUser cachedCurrent;

      lock (mutex) {
        cachedCurrent = CurrentUser;
      }

      if (cachedCurrent != null) {
        return Task<AVUser>.FromResult(cachedCurrent);
      }

      return taskQueue.Enqueue(toAwait => {
        return toAwait.ContinueWith(_ => {
          return storageController.LoadAsync().OnSuccess(t => {
            object temp;
            t.Result.TryGetValue("CurrentUser", out temp);
            var userDataString = temp as string;
            AVUser user = null;
            if (userDataString != null) {
              var userData =  Json.Parse(userDataString) as IDictionary<string, object>;
              var state = AVObjectCoder.Instance.Decode(userData, AVDecoder.Instance);
              user = AVObject.FromState<AVUser>(state, "_User");
            }

            CurrentUser = user;
            return user;
          });
        }).Unwrap();
      }, cancellationToken);
    }

    public Task<bool> ExistsAsync(CancellationToken cancellationToken) {
      if (CurrentUser != null) {
        return Task<bool>.FromResult(true);
      }

      return taskQueue.Enqueue(toAwait => {
        return toAwait.ContinueWith(_ =>
          storageController.LoadAsync().OnSuccess(t => t.Result.ContainsKey("CurrentUser"))
        ).Unwrap();
      }, cancellationToken);
    }

    public bool IsCurrent(AVUser user) {
      lock (mutex) {
        return CurrentUser == user;
      }
    }

    public void ClearFromMemory() {
      CurrentUser = null;
    }

    public void ClearFromDisk() {
      lock (mutex) {
        ClearFromMemory();

        taskQueue.Enqueue(toAwait => {
          return toAwait.ContinueWith(_ => {
            return storageController.LoadAsync().OnSuccess(t => t.Result.RemoveAsync("CurrentUser"));
          }).Unwrap().Unwrap();
        }, CancellationToken.None);
      }
    }

    public Task<string> GetCurrentSessionTokenAsync(CancellationToken cancellationToken) {
      return GetAsync(cancellationToken).OnSuccess(t => {
        var user = t.Result;
        return user == null ? null : user.SessionToken;
      });
    }

    public Task LogOutAsync(CancellationToken cancellationToken) {
      return taskQueue.Enqueue(toAwait => {
        return toAwait.ContinueWith(_ => GetAsync(cancellationToken)).Unwrap().OnSuccess(t => {
          ClearFromDisk();
        });
      }, cancellationToken);
    }
  }
}
