// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using Parse.Common.Internal;
using Parse.Core.Internal;

namespace Parse.Push.Internal
{
    internal class ParsePushController : IParsePushController
    {
        private readonly IParseCommandRunner commandRunner;
        private readonly IParseCurrentUserController currentUserController;

        public ParsePushController(IParseCommandRunner commandRunner, IParseCurrentUserController currentUserController)
        {
            this.commandRunner = commandRunner;
            this.currentUserController = currentUserController;
        }

        public Task SendPushNotificationAsync(IPushState state, CancellationToken cancellationToken)
        {
            return currentUserController.GetCurrentSessionTokenAsync(cancellationToken).OnSuccess(sessionTokenTask =>
            {
                var command = new ParseCommand("push",
                    method: "POST",
                    sessionToken: sessionTokenTask.Result,
                    data: ParsePushEncoder.Instance.Encode(state));

                return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken);
            }).Unwrap();
        }
    }
}
