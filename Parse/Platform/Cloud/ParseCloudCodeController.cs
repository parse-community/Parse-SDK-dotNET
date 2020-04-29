// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Cloud;
using Parse.Infrastructure.Utilities;
using Parse.Infrastructure.Data;
using Parse.Infrastructure.Execution;

namespace Parse.Platform.Cloud
{
    public class ParseCloudCodeController : IParseCloudCodeController
    {
        IParseCommandRunner CommandRunner { get; }

        IParseDataDecoder Decoder { get; }

        public ParseCloudCodeController(IParseCommandRunner commandRunner, IParseDataDecoder decoder) => (CommandRunner, Decoder) = (commandRunner, decoder);

        public Task<T> CallFunctionAsync<T>(string name, IDictionary<string, object> parameters, string sessionToken, IServiceHub serviceHub, CancellationToken cancellationToken = default) => CommandRunner.RunCommandAsync(new ParseCommand($"functions/{Uri.EscapeUriString(name)}", method: "POST", sessionToken: sessionToken, data: NoObjectsEncoder.Instance.Encode(parameters, serviceHub) as IDictionary<string, object>), cancellationToken: cancellationToken).OnSuccess(task =>
        {
            IDictionary<string, object> decoded = Decoder.Decode(task.Result.Item2, serviceHub) as IDictionary<string, object>;
            return !decoded.ContainsKey("result") ? default : Conversion.To<T>(decoded["result"]);
        });
    }
}
