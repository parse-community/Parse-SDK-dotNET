// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Configuration;
using Parse.Infrastructure.Utilities;
using Parse;
using Parse.Infrastructure.Execution;

namespace Parse.Platform.Configuration
{
    /// <summary>
    /// Config controller.
    /// </summary>
    internal class ParseConfigurationController : IParseConfigurationController
    {
        IParseCommandRunner CommandRunner { get; }

        IParseDataDecoder Decoder { get; }

        public IParseCurrentConfigurationController CurrentConfigurationController { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParseConfigurationController"/> class.
        /// </summary>
        public ParseConfigurationController(IParseCommandRunner commandRunner, ICacheController storageController, IParseDataDecoder decoder)
        {
            CommandRunner = commandRunner;
            CurrentConfigurationController = new ParseCurrentConfigurationController(storageController, decoder);
            Decoder = decoder;
        }

        public Task<ParseConfiguration> FetchConfigAsync(string sessionToken, IServiceHub serviceHub, CancellationToken cancellationToken = default) => CommandRunner.RunCommandAsync(new ParseCommand("config", method: "GET", sessionToken: sessionToken, data: default), cancellationToken: cancellationToken).OnSuccess(task =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Decoder.BuildConfiguration(task.Result.Item2, serviceHub);
        }).OnSuccess(task =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            CurrentConfigurationController.SetCurrentConfigAsync(task.Result);
            return task;
        }).Unwrap();
    }
}
