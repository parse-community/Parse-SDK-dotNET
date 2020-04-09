// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Library;
using Parse.Common.Internal;

namespace Parse.Core.Internal
{
    /// <summary>
    /// Parse current config controller.
    /// </summary>
    internal class ParseCurrentConfigurationController : IParseCurrentConfigurationController
    {
        static string CurrentConfigurationKey { get; } = "CurrentConfig";

        TaskQueue TaskQueue { get; }

        ParseConfiguration CurrentConfiguration { get; set; }

        IStorageController StorageController { get; }

        IParseDataDecoder Decoder { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParseCurrentConfigurationController"/> class.
        /// </summary>
        public ParseCurrentConfigurationController(IStorageController storageController, IParseDataDecoder decoder)
        {
            StorageController = storageController;
            Decoder = decoder;
            TaskQueue = new TaskQueue { };
        }

        public Task<ParseConfiguration> GetCurrentConfigAsync(IServiceHub serviceHub) => TaskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ => CurrentConfiguration is { } ? Task.FromResult(CurrentConfiguration) : StorageController.LoadAsync().OnSuccess(task =>
        {
            task.Result.TryGetValue(CurrentConfigurationKey, out object data);
            return CurrentConfiguration = data is string { } configuration ? Decoder.BuildConfiguration(ParseClient.DeserializeJsonString(configuration), serviceHub) : new ParseConfiguration(serviceHub);
        })), CancellationToken.None).Unwrap();

        public Task SetCurrentConfigAsync(ParseConfiguration target) => TaskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ =>
        {
            CurrentConfiguration = target;
            return StorageController.LoadAsync().OnSuccess(task => task.Result.AddAsync(CurrentConfigurationKey, ParseClient.SerializeJsonString(((IJsonConvertible) target).ConvertToJSON())));
        }).Unwrap().Unwrap(), CancellationToken.None);

        public Task ClearCurrentConfigAsync() => TaskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ =>
        {
            CurrentConfiguration = null;
            return StorageController.LoadAsync().OnSuccess(task => task.Result.RemoveAsync(CurrentConfigurationKey));
        }).Unwrap().Unwrap(), CancellationToken.None);

        public Task ClearCurrentConfigInMemoryAsync() => TaskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ => CurrentConfiguration = null), CancellationToken.None);
    }
}
