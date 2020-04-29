// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Sessions;
using Parse.Infrastructure.Utilities;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure.Execution;
using Parse.Infrastructure.Data;

namespace Parse.Platform.Sessions
{
    public class ParseSessionController : IParseSessionController
    {
        IParseCommandRunner CommandRunner { get; }

        IParseDataDecoder Decoder { get; }

        public ParseSessionController(IParseCommandRunner commandRunner, IParseDataDecoder decoder) => (CommandRunner, Decoder) = (commandRunner, decoder);

        public Task<IObjectState> GetSessionAsync(string sessionToken, IServiceHub serviceHub, CancellationToken cancellationToken = default) => CommandRunner.RunCommandAsync(new ParseCommand("sessions/me", method: "GET", sessionToken: sessionToken, data: null), cancellationToken: cancellationToken).OnSuccess(task => ParseObjectCoder.Instance.Decode(task.Result.Item2, Decoder, serviceHub));

        public Task RevokeAsync(string sessionToken, CancellationToken cancellationToken = default) => CommandRunner.RunCommandAsync(new ParseCommand("logout", method: "POST", sessionToken: sessionToken, data: new Dictionary<string, object> { }), cancellationToken: cancellationToken);

        public Task<IObjectState> UpgradeToRevocableSessionAsync(string sessionToken, IServiceHub serviceHub, CancellationToken cancellationToken = default) => CommandRunner.RunCommandAsync(new ParseCommand("upgradeToRevocableSession", method: "POST", sessionToken: sessionToken, data: new Dictionary<string, object>()), cancellationToken: cancellationToken).OnSuccess(task => ParseObjectCoder.Instance.Decode(task.Result.Item2, Decoder, serviceHub));

        public bool IsRevocableSessionToken(string sessionToken) => sessionToken.Contains("r:");
    }
}
