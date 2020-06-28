// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Platform.Push;
using Parse.Abstractions.Platform.Users;
using Parse.Infrastructure.Execution;
using Parse.Infrastructure.Utilities;

namespace Parse.Platform.Push
{
    internal class ParsePushController : IParsePushController
    {
        IParseCommandRunner CommandRunner { get; }

        IParseCurrentUserController CurrentUserController { get; }

        public ParsePushController(IParseCommandRunner commandRunner, IParseCurrentUserController currentUserController)
        {
            CommandRunner = commandRunner;
            CurrentUserController = currentUserController;
        }

        public Task SendPushNotificationAsync(IPushState state, IServiceHub serviceHub, CancellationToken cancellationToken = default) => CurrentUserController.GetCurrentSessionTokenAsync(serviceHub, cancellationToken).OnSuccess(sessionTokenTask => CommandRunner.RunCommandAsync(new ParseCommand("push", method: "POST", sessionToken: sessionTokenTask.Result, data: ParsePushEncoder.Instance.Encode(state)), cancellationToken: cancellationToken)).Unwrap();
    }
}
