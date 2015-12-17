// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Parse.Internal {
  class ParseCurrentUserController : IParseCurrentUserController {
    private readonly object mutex = new object();
    private readonly TaskQueue taskQueue = new TaskQueue();

    private ParseUser currentUser;
    internal ParseUser CurrentUser {
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

    public Task SetAsync(ParseUser user, CancellationToken cancellationToken) {
      return taskQueue.Enqueue(toAwait => {
        return toAwait.ContinueWith(_ => {
          if (user == null) {
            ParseClient.ApplicationSettings.Remove("CurrentUser");
          } else {
            // TODO (hallucinogen): we need to use ParseCurrentCoder instead of this janky encoding
            var data = user.ServerDataToJSONObjectForSerialization();
            data["objectId"] = user.ObjectId;
            if (user.CreatedAt != null) {
              data["createdAt"] = user.CreatedAt.Value.ToString(ParseClient.DateFormatStrings.First());
            }
            if (user.UpdatedAt != null) {
              data["updatedAt"] = user.UpdatedAt.Value.ToString(ParseClient.DateFormatStrings.First());
            }

            ParseClient.ApplicationSettings["CurrentUser"] = Json.Encode(data);
          }
          CurrentUser = user;
        });
      }, cancellationToken);
    }

    public Task<ParseUser> GetAsync(CancellationToken cancellationToken) {
      ParseUser cachedCurrent;

      lock (mutex) {
        cachedCurrent = CurrentUser;
      }

      if (cachedCurrent != null) {
        return Task<ParseUser>.FromResult(cachedCurrent);
      }

      return taskQueue.Enqueue(toAwait => {
        return toAwait.ContinueWith(t => {
          object temp;
          ParseClient.ApplicationSettings.TryGetValue("CurrentUser", out temp);
          var userDataString = temp as string;
          ParseUser user = null;
          if (userDataString != null) {
            var userData =  Json.Parse(userDataString) as IDictionary<string, object>;
            var state = ParseObjectCoder.Instance.Decode(userData, ParseDecoder.Instance);
            user = ParseObject.FromState<ParseUser>(state, "_User");
          }

          CurrentUser = user;
          return user;
        });
      }, cancellationToken);
    }

    public Task<bool> ExistsAsync(CancellationToken cancellationToken) {
      if (CurrentUser != null) {
        return Task<bool>.FromResult(true);
      }

      return taskQueue.Enqueue(toAwait => {
        return toAwait.ContinueWith(t => ParseClient.ApplicationSettings.ContainsKey("CurrentUser"));
      }, cancellationToken);
    }

    public bool IsCurrent(ParseUser user) {
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

        ParseClient.ApplicationSettings.Remove("CurrentUser");
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
