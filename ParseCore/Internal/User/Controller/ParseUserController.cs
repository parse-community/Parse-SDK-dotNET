// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse.Common.Internal;

namespace Parse.Core.Internal {
  public class ParseUserController : IParseUserController {
    private readonly IParseCommandRunner commandRunner;

    public ParseUserController(IParseCommandRunner commandRunner) {
      this.commandRunner = commandRunner;
    }

    public Task<IObjectState> SignUpAsync(IObjectState state,
        IDictionary<string, IParseFieldOperation> operations,
        CancellationToken cancellationToken) {
      var objectJSON = ParseObject.ToJSONObjectForSaving(operations);

      var command = new ParseCommand("classes/_User",
          method: "POST",
          data: objectJSON);

      return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t => {
        var serverState = ParseObjectCoder.Instance.Decode(t.Result.Item2, ParseDecoder.Instance);
        serverState = serverState.MutatedClone(mutableClone => {
          mutableClone.IsNew = true;
        });
        return serverState;
      });
    }

    public Task<IObjectState> LogInAsync(string username,
        string password,
        CancellationToken cancellationToken) {
      var data = new Dictionary<string, object>{
        {"username", username},
        {"password", password}
      };

      var command = new ParseCommand(string.Format("login?{0}", ParseClient.BuildQueryString(data)),
          method: "GET",
          data: null);

      return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t => {
        var serverState = ParseObjectCoder.Instance.Decode(t.Result.Item2, ParseDecoder.Instance);
        serverState = serverState.MutatedClone(mutableClone => {
          mutableClone.IsNew = t.Result.Item1 == System.Net.HttpStatusCode.Created;
        });
        return serverState;
      });
    }

    public Task<IObjectState> LogInAsync(string authType,
        IDictionary<string, object> data,
        CancellationToken cancellationToken) {
      var authData = new Dictionary<string, object>();
      authData[authType] = data;

      var command = new ParseCommand("users",
          method: "POST",
          data: new Dictionary<string, object> {
            {"authData", authData}
          });

      return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t => {
        var serverState = ParseObjectCoder.Instance.Decode(t.Result.Item2, ParseDecoder.Instance);
        serverState = serverState.MutatedClone(mutableClone => {
          mutableClone.IsNew = t.Result.Item1 == System.Net.HttpStatusCode.Created;
        });
        return serverState;
      });
    }

    public Task<IObjectState> GetUserAsync(string sessionToken, CancellationToken cancellationToken) {
      var command = new ParseCommand("users/me",
          method: "GET",
          sessionToken: sessionToken,
          data: null);

      return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t => {
        return ParseObjectCoder.Instance.Decode(t.Result.Item2, ParseDecoder.Instance);
      });
    }

    public Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken) {
      var command = new ParseCommand("requestPasswordReset",
          method: "POST",
          data: new Dictionary<string, object> {
            {"email", email}
          });

      return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken);
    }
  }
}
