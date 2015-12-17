// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Parse.Internal {
  internal class ParseCurrentInstallationController : IParseCurrentInstallationController {
    private readonly object mutex = new object();
    private readonly TaskQueue taskQueue = new TaskQueue();
    private readonly IInstallationIdController installationIdController;

    public ParseCurrentInstallationController(IInstallationIdController installationIdController) {
      this.installationIdController = installationIdController;
    }

    private ParseInstallation currentInstallation;
    internal ParseInstallation CurrentInstallation {
      get {
        lock (mutex) {
          return currentInstallation;
        }
      }
      set {
        lock (mutex) {
          currentInstallation = value;
        }
      }
    }

    public Task SetAsync(ParseInstallation installation, CancellationToken cancellationToken) {
      return taskQueue.Enqueue(toAwait => {
        return toAwait.ContinueWith(_ => {
          if (installation == null) {
            ParseClient.ApplicationSettings.Remove("CurrentInstallation");
          } else {
            // TODO (hallucinogen): we need to use ParseCurrentCoder instead of this janky encoding
            var data = installation.ServerDataToJSONObjectForSerialization();
            data["objectId"] = installation.ObjectId;
            if (installation.CreatedAt != null) {
              data["createdAt"] = installation.CreatedAt.Value.ToString(ParseClient.DateFormatStrings.First());
            }
            if (installation.UpdatedAt != null) {
              data["updatedAt"] = installation.UpdatedAt.Value.ToString(ParseClient.DateFormatStrings.First());
            }

            ParseClient.ApplicationSettings["CurrentInstallation"] = Json.Encode(data);
          }
          CurrentInstallation = installation;
        });
      }, cancellationToken);
    }

    public Task<ParseInstallation> GetAsync(CancellationToken cancellationToken) {
      ParseInstallation cachedCurrent;
      cachedCurrent = CurrentInstallation;

      if (cachedCurrent != null) {
        return Task<ParseInstallation>.FromResult(cachedCurrent);
      }

      return taskQueue.Enqueue(toAwait => {
        return toAwait.ContinueWith(t => {
          object temp;
          ParseClient.ApplicationSettings.TryGetValue("CurrentInstallation", out temp);
          var installationDataString = temp as string;
          ParseInstallation installation = null;
          if (installationDataString != null) {
            var installationData = ParseClient.DeserializeJsonString(installationDataString);
            var state = ParseObjectCoder.Instance.Decode(installationData, ParseDecoder.Instance);
            installation = ParseObject.FromState<ParseInstallation>(state, "_Installation");
          } else {
            installation = ParseObject.Create<ParseInstallation>();
            installation.SetIfDifferent("installationId" , installationIdController.Get().ToString());
          }

          CurrentInstallation = installation;
          return installation;
        });
      }, cancellationToken);
    }

    public Task<bool> ExistsAsync(CancellationToken cancellationToken) {
      if (CurrentInstallation != null) {
        return Task<bool>.FromResult(true);
      }

      return taskQueue.Enqueue(toAwait => {
        return toAwait.ContinueWith(t => ParseClient.ApplicationSettings.ContainsKey("CurrentInstallation"));
      }, cancellationToken);
    }

    public bool IsCurrent(ParseInstallation installation) {
      return CurrentInstallation == installation;
    }

    public void ClearFromMemory() {
      CurrentInstallation = null;
    }

    public void ClearFromDisk() {
      ClearFromMemory();

      ParseClient.ApplicationSettings.Remove("CurrentInstallation");
    }
  }
}
