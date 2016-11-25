// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using LeanCloud.Common.Internal;
using LeanCloud.Core.Internal;

namespace LeanCloud.Push.Internal {
  internal class AVPushController : IAVPushController {
    private readonly IAVCommandRunner commandRunner;
    private readonly IAVCurrentUserController currentUserController;

    public AVPushController(IAVCommandRunner commandRunner, IAVCurrentUserController currentUserController) {
      this.commandRunner = commandRunner;
      this.currentUserController = currentUserController;
    }

    public Task SendPushNotificationAsync(IAVState state, CancellationToken cancellationToken) {
      return currentUserController.GetCurrentSessionTokenAsync(cancellationToken).OnSuccess(sessionTokenTask => {
        var command = new AVCommand("push",
            method: "POST",
            sessionToken: sessionTokenTask.Result,
            data: AVPushEncoder.Instance.Encode(state));

        return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken);
      }).Unwrap();
    }
  }
}
