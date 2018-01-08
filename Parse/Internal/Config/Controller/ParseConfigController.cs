// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Threading.Tasks;
using System.Threading;
using Parse.Common.Internal;

namespace Parse.Core.Internal {
  /// <summary>
  /// Config controller.
  /// </summary>
  internal class ParseConfigController : IParseConfigController {
    private readonly IParseCommandRunner commandRunner;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParseConfigController"/> class.
    /// </summary>
    public ParseConfigController(IParseCommandRunner commandRunner, IStorageController storageController) {
      this.commandRunner = commandRunner;
      CurrentConfigController = new ParseCurrentConfigController(storageController);
    }

    public IParseCommandRunner CommandRunner { get; internal set; }
    public IParseCurrentConfigController CurrentConfigController { get; internal set; }

    public Task<ParseConfig> FetchConfigAsync(String sessionToken, CancellationToken cancellationToken) {
      var command = new ParseCommand("config",
          method: "GET",
          sessionToken: sessionToken,
          data: null);

      return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(task => {
        cancellationToken.ThrowIfCancellationRequested();
        return new ParseConfig(task.Result.Item2);
      }).OnSuccess(task => {
        cancellationToken.ThrowIfCancellationRequested();
        CurrentConfigController.SetCurrentConfigAsync(task.Result);
        return task;
      }).Unwrap();
    }
  }
}
