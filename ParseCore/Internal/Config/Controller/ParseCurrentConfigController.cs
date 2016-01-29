// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using Parse.Common.Internal;

namespace Parse.Core.Internal {
  /// <summary>
  /// Parse current config controller.
  /// </summary>
  internal class ParseCurrentConfigController : IParseCurrentConfigController {
    private const string CurrentConfigKey = "CurrentConfig";

    private readonly TaskQueue taskQueue;
    private ParseConfig currentConfig;

    private IStorageController storageController;

    /// <summary>
    /// Initializes a new instance of the <see cref="Parse.Core.Internal.ParseCurrentConfigController"/> class.
    /// </summary>
    public ParseCurrentConfigController(IStorageController storageController) {
      this.storageController = storageController;

      taskQueue = new TaskQueue();
    }

    public Task<ParseConfig> GetCurrentConfigAsync() {
      return taskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ => {
        if (currentConfig == null) {
          return storageController.LoadAsync().OnSuccess(t => {
            object tmp;
            t.Result.TryGetValue(CurrentConfigKey, out tmp);

            string propertiesString = tmp as string;
            if (propertiesString != null) {
              var dictionary = ParseClient.DeserializeJsonString(propertiesString);
              currentConfig = new ParseConfig(dictionary);
            } else {
              currentConfig = new ParseConfig();
            }

            return currentConfig;
          });
        }

        return Task.FromResult(currentConfig);
      }), CancellationToken.None).Unwrap();
    }

    public Task SetCurrentConfigAsync(ParseConfig config) {
      return taskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ => {
        currentConfig = config;

        var jsonObject = ((IJsonConvertible)config).ToJSON();
        var jsonString = ParseClient.SerializeJsonString(jsonObject);

        return storageController.LoadAsync().OnSuccess(t => t.Result.AddAsync(CurrentConfigKey, jsonString));
      }).Unwrap().Unwrap(), CancellationToken.None);
    }

    public Task ClearCurrentConfigAsync() {
      return taskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ => {
        currentConfig = null;

        return storageController.LoadAsync().OnSuccess(t => t.Result.RemoveAsync(CurrentConfigKey));
      }).Unwrap().Unwrap(), CancellationToken.None);
    }

    public Task ClearCurrentConfigInMemoryAsync() {
      return taskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ => {
        currentConfig = null;
      }), CancellationToken.None);
    }
  }
}
