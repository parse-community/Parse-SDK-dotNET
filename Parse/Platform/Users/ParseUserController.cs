// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Users;
using Parse.Infrastructure.Utilities;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure.Execution;
using Parse.Infrastructure.Data;

namespace Parse.Platform.Users
{
    public class ParseUserController : IParseUserController
    {
        IParseCommandRunner CommandRunner { get; }

        IParseDataDecoder Decoder { get; }

        public bool RevocableSessionEnabled { get; set; }

        public object RevocableSessionEnabledMutex { get; } = new object { };

        public ParseUserController(IParseCommandRunner commandRunner, IParseDataDecoder decoder) => (CommandRunner, Decoder) = (commandRunner, decoder);

        public Task<IObjectState> SignUpAsync(IObjectState state, IDictionary<string, IParseFieldOperation> operations, IServiceHub serviceHub, CancellationToken cancellationToken = default) => CommandRunner.RunCommandAsync(new ParseCommand("classes/_User", method: "POST", data: serviceHub.GenerateJSONObjectForSaving(operations)), cancellationToken: cancellationToken).OnSuccess(task => ParseObjectCoder.Instance.Decode(task.Result.Item2, Decoder, serviceHub).MutatedClone(mutableClone => mutableClone.IsNew = true));

        public Task<IObjectState> LogInAsync(string username, string password, IServiceHub serviceHub, CancellationToken cancellationToken = default) => CommandRunner.RunCommandAsync(new ParseCommand($"login?{ParseClient.BuildQueryString(new Dictionary<string, object> { [nameof(username)] = username, [nameof(password)] = password })}", method: "GET", data: null), cancellationToken: cancellationToken).OnSuccess(task => ParseObjectCoder.Instance.Decode(task.Result.Item2, Decoder, serviceHub).MutatedClone(mutableClone => mutableClone.IsNew = task.Result.Item1 == System.Net.HttpStatusCode.Created));

        public Task<IObjectState> LogInAsync(string authType, IDictionary<string, object> data, IServiceHub serviceHub, CancellationToken cancellationToken = default)
        {
            Dictionary<string, object> authData = new Dictionary<string, object>
            {
                [authType] = data
            };

            return CommandRunner.RunCommandAsync(new ParseCommand("users", method: "POST", data: new Dictionary<string, object> { [nameof(authData)] = authData }), cancellationToken: cancellationToken).OnSuccess(task => ParseObjectCoder.Instance.Decode(task.Result.Item2, Decoder, serviceHub).MutatedClone(mutableClone => mutableClone.IsNew = task.Result.Item1 == System.Net.HttpStatusCode.Created));
        }

        public Task<IObjectState> GetUserAsync(string sessionToken, IServiceHub serviceHub, CancellationToken cancellationToken = default) => CommandRunner.RunCommandAsync(new ParseCommand("users/me", method: "GET", sessionToken: sessionToken, data: default), cancellationToken: cancellationToken).OnSuccess(task => ParseObjectCoder.Instance.Decode(task.Result.Item2, Decoder, serviceHub));

        public Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default) => CommandRunner.RunCommandAsync(new ParseCommand("requestPasswordReset", method: "POST", data: new Dictionary<string, object> { [nameof(email)] = email }), cancellationToken: cancellationToken);
    }
}
