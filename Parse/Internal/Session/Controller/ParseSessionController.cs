// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse.Common.Internal;

namespace Parse.Core.Internal {
  public class ParseSessionController : IParseSessionController {
    private readonly IParseCommandRunner commandRunner;

    public ParseSessionController(IParseCommandRunner commandRunner) {
      this.commandRunner = commandRunner;
    }

    public Task<IObjectState> GetSessionAsync(string sessionToken, CancellationToken cancellationToken) {
      var command = new ParseCommand("sessions/me",
          method: "GET",
          sessionToken: sessionToken,
          data: null);

      return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t => {
        return ParseObjectCoder.Instance.Decode(t.Result.Item2, ParseDecoder.Instance);
      });
    }

    public Task RevokeAsync(string sessionToken, CancellationToken cancellationToken) {
      var command = new ParseCommand("logout",
          method: "POST",
          sessionToken: sessionToken,
          data: new Dictionary<string, object>());

      return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken);
    }

    public Task<IObjectState> UpgradeToRevocableSessionAsync(string sessionToken, CancellationToken cancellationToken) {
      var command = new ParseCommand("upgradeToRevocableSession",
          method: "POST",
          sessionToken: sessionToken,
          data: new Dictionary<string, object>());

      return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t => {
        return ParseObjectCoder.Instance.Decode(t.Result.Item2, ParseDecoder.Instance);
      });
    }

    public bool IsRevocableSessionToken(string sessionToken) {
      return sessionToken.Contains("r:");
    }
  }
}
