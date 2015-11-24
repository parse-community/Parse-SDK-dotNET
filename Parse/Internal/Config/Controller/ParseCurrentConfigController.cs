// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace Parse.Internal {
  /// <summary>
  /// Parse current config controller.
  /// </summary>
  class ParseCurrentConfigController : IParseCurrentConfigController {
    private const string CurrentConfigKey = "CurrentConfig";

    private readonly TaskQueue taskQueue;
    private ParseConfig currentConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="Parse.Internal.ParseCurrentConfigController"/> class.
    /// </summary>
    public ParseCurrentConfigController() {
      taskQueue = new TaskQueue();
    }

    public Task<ParseConfig> GetCurrentConfigAsync() {
      return taskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ => {
        if (currentConfig == null) {
          object tmp;
          ParseClient.ApplicationSettings.TryGetValue(CurrentConfigKey, out tmp);

          string propertiesString = tmp as string;
          if (propertiesString != null) {
            var dictionary = ParseClient.DeserializeJsonString(propertiesString);
            currentConfig = new ParseConfig(dictionary);
          } else {
            currentConfig = new ParseConfig();
          }
        }

        return currentConfig;
      }), CancellationToken.None);
    }

    public Task SetCurrentConfigAsync(ParseConfig config) {
      return taskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ => { 
        currentConfig = config;

        var jsonObject = ((IJsonConvertible)config).ToJSON();
        var jsonString = ParseClient.SerializeJsonString(jsonObject);

        ParseClient.ApplicationSettings[CurrentConfigKey] = jsonString;
      }), CancellationToken.None);
    }

    public Task ClearCurrentConfigAsync() {
      return taskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ => {
        currentConfig = null;
        ParseClient.ApplicationSettings.Remove(CurrentConfigKey);
      }), CancellationToken.None);
    }

    public Task ClearCurrentConfigInMemoryAsync() {
      return taskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ => {
        currentConfig = null;
      }), CancellationToken.None);
    }
  }
}
