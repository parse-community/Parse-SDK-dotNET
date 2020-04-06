// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse.Common.Internal;

namespace Parse.Core.Internal
{
    public class ParseSessionController : IParseSessionController
    {
        IParseCommandRunner CommandRunner { get; }

        IParseDataDecoder Decoder { get; }

        public ParseSessionController(IParseCommandRunner commandRunner, IParseDataDecoder decoder) => (CommandRunner, Decoder) = (commandRunner, decoder);

        public Task<IObjectState> GetSessionAsync(string sessionToken, CancellationToken cancellationToken) => CommandRunner.RunCommandAsync(new ParseCommand("sessions/me", method: "GET", sessionToken: sessionToken, data: null), cancellationToken: cancellationToken).OnSuccess(task => ParseObjectCoder.Instance.Decode(task.Result.Item2, Decoder));

        public Task RevokeAsync(string sessionToken, CancellationToken cancellationToken) => CommandRunner.RunCommandAsync(new ParseCommand("logout", method: "POST", sessionToken: sessionToken, data: new Dictionary<string, object> { }), cancellationToken: cancellationToken);

        public Task<IObjectState> UpgradeToRevocableSessionAsync(string sessionToken, CancellationToken cancellationToken) => CommandRunner.RunCommandAsync(new ParseCommand("upgradeToRevocableSession", method: "POST", sessionToken: sessionToken, data: new Dictionary<string, object>()), cancellationToken: cancellationToken).OnSuccess(task => ParseObjectCoder.Instance.Decode(task.Result.Item2, Decoder));

        public bool IsRevocableSessionToken(string sessionToken) => sessionToken.Contains("r:");
    }
}
