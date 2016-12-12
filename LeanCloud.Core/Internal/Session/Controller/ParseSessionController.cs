// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Storage.Internal;

namespace LeanCloud.Core.Internal {
  public class AVSessionController : IAVSessionController {
    private readonly IAVCommandRunner commandRunner;

    public AVSessionController(IAVCommandRunner commandRunner) {
      this.commandRunner = commandRunner;
    }

    public Task<IObjectState> GetSessionAsync(string sessionToken, CancellationToken cancellationToken) {
      var command = new AVCommand("sessions/me",
          method: "GET",
          sessionToken: sessionToken,
          data: null);

      return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t => {
        return AVObjectCoder.Instance.Decode(t.Result.Item2, AVDecoder.Instance);
      });
    }

    public Task RevokeAsync(string sessionToken, CancellationToken cancellationToken) {
      var command = new AVCommand("logout",
          method: "POST",
          sessionToken: sessionToken,
          data: new Dictionary<string, object>());

      return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken);
    }

    public Task<IObjectState> UpgradeToRevocableSessionAsync(string sessionToken, CancellationToken cancellationToken) {
      var command = new AVCommand("upgradeToRevocableSession",
          method: "POST",
          sessionToken: sessionToken,
          data: new Dictionary<string, object>());

      return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t => {
        return AVObjectCoder.Instance.Decode(t.Result.Item2, AVDecoder.Instance);
      });
    }

    public bool IsRevocableSessionToken(string sessionToken) {
      return sessionToken.Contains("r:");
    }
  }
}
