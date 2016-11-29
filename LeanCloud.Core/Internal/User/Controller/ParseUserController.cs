// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Storage.Internal;

namespace LeanCloud.Core.Internal
{
    public class AVUserController : IAVUserController
    {
        private readonly IAVCommandRunner commandRunner;

        public AVUserController(IAVCommandRunner commandRunner)
        {
            this.commandRunner = commandRunner;
        }

        public Task<IObjectState> SignUpAsync(IObjectState state,
            IDictionary<string, IAVFieldOperation> operations,
            CancellationToken cancellationToken)
        {
            var objectJSON = AVObject.ToJSONObjectForSaving(operations);

            var command = new AVCommand("classes/_User",
                method: "POST",
                data: objectJSON);

            return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t =>
            {
                var serverState = AVObjectCoder.Instance.Decode(t.Result.Item2, AVDecoder.Instance);
                serverState = serverState.MutatedClone(mutableClone =>
                {
                    mutableClone.IsNew = true;
                });
                return serverState;
            });
        }

        public Task<IObjectState> LogInAsync(string username,
            string password,
            CancellationToken cancellationToken)
        {
            var data = new Dictionary<string, object>{
                { "username", username},
                { "password", password}
            };

            var command = new AVCommand(string.Format("login?{0}", AVClient.BuildQueryString(data)),
                method: "GET",
                data: null);

            return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t =>
            {
                var serverState = AVObjectCoder.Instance.Decode(t.Result.Item2, AVDecoder.Instance);
                serverState = serverState.MutatedClone(mutableClone =>
                {
                    mutableClone.IsNew = t.Result.Item1 == System.Net.HttpStatusCode.Created;
                });
                return serverState;
            });
        }

        public Task<IObjectState> LogInAsync(string authType,
            IDictionary<string, object> data,
            CancellationToken cancellationToken)
        {
            var authData = new Dictionary<string, object>();
            authData[authType] = data;

            var command = new AVCommand("users",
                method: "POST",
                data: new Dictionary<string, object> {
                    { "authData", authData}
                });

            return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t =>
            {
                var serverState = AVObjectCoder.Instance.Decode(t.Result.Item2, AVDecoder.Instance);
                serverState = serverState.MutatedClone(mutableClone =>
                {
                    mutableClone.IsNew = t.Result.Item1 == System.Net.HttpStatusCode.Created;
                });
                return serverState;
            });
        }

        public Task<IObjectState> GetUserAsync(string sessionToken, CancellationToken cancellationToken)
        {
            var command = new AVCommand("users/me",
                method: "GET",
                sessionToken: sessionToken,
                data: null);

            return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t =>
            {
                return AVObjectCoder.Instance.Decode(t.Result.Item2, AVDecoder.Instance);
            });
        }

        public Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken)
        {
            var command = new AVCommand("requestPasswordReset",
                method: "POST",
                data: new Dictionary<string, object> {
                    { "email", email}
                });

            return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken);
        }

        public Task<IObjectState> LogInWithParametersAsync(string relativeUrl, IDictionary<string, object> data,
            CancellationToken cancellationToken)
        {
            var command = new AVCommand(string.Format("{0}", relativeUrl),
                method: "POST",
                data: data);

            return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t =>
            {
                var serverState = AVObjectCoder.Instance.Decode(t.Result.Item2, AVDecoder.Instance);
                serverState = serverState.MutatedClone(mutableClone =>
                {
                    mutableClone.IsNew = t.Result.Item1 == System.Net.HttpStatusCode.Created;
                });
                return serverState;
            });
        }

    }
}
