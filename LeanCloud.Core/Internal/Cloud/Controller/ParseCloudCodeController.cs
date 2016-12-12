// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Utilities;
using LeanCloud.Storage.Internal;

namespace LeanCloud.Core.Internal
{
    public class AVCloudCodeController : IAVCloudCodeController
    {
        private readonly IAVCommandRunner commandRunner;

        public AVCloudCodeController(IAVCommandRunner commandRunner)
        {
            this.commandRunner = commandRunner;
        }

        public Task<T> CallFunctionAsync<T>(String name,
            IDictionary<string, object> parameters,
            string sessionToken,
            CancellationToken cancellationToken)
        {
            var command = new AVCommand(string.Format("functions/{0}", Uri.EscapeUriString(name)),
                method: "POST",
                sessionToken: sessionToken,
                data: NoObjectsEncoder.Instance.Encode(parameters) as IDictionary<string, object>);

            return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t =>
            {
                var decoded = AVDecoder.Instance.Decode(t.Result.Item2) as IDictionary<string, object>;
                if (!decoded.ContainsKey("result"))
                {
                    return default(T);
                }
                return Conversion.To<T>(decoded["result"]);
            });
        }

        public Task<T> RPCFunction<T>(string name, IDictionary<string, object> parameters, string sessionToken, CancellationToken cancellationToken)
        {
            var command = new AVCommand(string.Format("call/{0}", Uri.EscapeUriString(name)),
                method: "POST",
                sessionToken: sessionToken,
                data: NoObjectsEncoder.Instance.Encode(parameters) as IDictionary<string, object>);

            return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t =>
            {
                var decoded = AVDecoder.Instance.Decode(t.Result.Item2) as IDictionary<string, object>;
                if (!decoded.ContainsKey("result"))
                {
                    return default(T);
                }
                return Conversion.To<T>(decoded["result"]);
            });
        }
    }
}
