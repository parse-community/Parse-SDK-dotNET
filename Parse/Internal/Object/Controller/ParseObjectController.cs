// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse.Utilities;
using Parse.Common.Internal;

namespace Parse.Core.Internal {
  public class ParseObjectController : IParseObjectController {
    private readonly IParseCommandRunner commandRunner;

    public ParseObjectController(IParseCommandRunner commandRunner) {
      this.commandRunner = commandRunner;
    }

    public Task<IObjectState> FetchAsync(IObjectState state,
        string sessionToken,
        CancellationToken cancellationToken) {
      var command = new ParseCommand(string.Format("classes/{0}/{1}",
              Uri.EscapeDataString(state.ClassName),
              Uri.EscapeDataString(state.ObjectId)),
          method: "GET",
          sessionToken: sessionToken,
          data: null);

      return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t => {
        return ParseObjectCoder.Instance.Decode(t.Result.Item2, ParseDecoder.Instance);
      });
    }

    public Task<IObjectState> SaveAsync(IObjectState state,
        IDictionary<string, IParseFieldOperation> operations,
        string sessionToken,
        CancellationToken cancellationToken) {
      var objectJSON = ParseObject.ToJSONObjectForSaving(operations);

      var command = new ParseCommand((state.ObjectId == null ?
              string.Format("classes/{0}", Uri.EscapeDataString(state.ClassName)) :
              string.Format("classes/{0}/{1}", Uri.EscapeDataString(state.ClassName), state.ObjectId)),
          method: (state.ObjectId == null ? "POST" : "PUT"),
          sessionToken: sessionToken,
          data: objectJSON);

      return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t => {
        var serverState = ParseObjectCoder.Instance.Decode(t.Result.Item2, ParseDecoder.Instance);
        serverState = serverState.MutatedClone(mutableClone => {
          mutableClone.IsNew = t.Result.Item1 == System.Net.HttpStatusCode.Created;
        });
        return serverState;
      });
    }

    public IList<Task<IObjectState>> SaveAllAsync(IList<IObjectState> states,
        IList<IDictionary<string, IParseFieldOperation>> operationsList,
        string sessionToken,
        CancellationToken cancellationToken) {

      var requests = states
        .Zip(operationsList, (item, ops) => new ParseCommand(
          item.ObjectId == null
            ? string.Format("classes/{0}", Uri.EscapeDataString(item.ClassName))
            : string.Format("classes/{0}/{1}", Uri.EscapeDataString(item.ClassName), Uri.EscapeDataString(item.ObjectId)),
          method: item.ObjectId == null ? "POST" : "PUT",
          data: ParseObject.ToJSONObjectForSaving(ops)))
        .ToList();

      var batchTasks = ExecuteBatchRequests(requests, sessionToken, cancellationToken);
      var stateTasks = new List<Task<IObjectState>>();
      foreach (var task in batchTasks) {
        stateTasks.Add(task.OnSuccess(t => {
          return ParseObjectCoder.Instance.Decode(t.Result, ParseDecoder.Instance);
        }));
      }

      return stateTasks;
    }

    public Task DeleteAsync(IObjectState state,
        string sessionToken,
        CancellationToken cancellationToken) {
      var command = new ParseCommand(string.Format("classes/{0}/{1}",
              state.ClassName, state.ObjectId),
          method: "DELETE",
          sessionToken: sessionToken,
          data: null);

      return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken);
    }

    public IList<Task> DeleteAllAsync(IList<IObjectState> states,
        string sessionToken,
        CancellationToken cancellationToken) {
      var requests = states
        .Where(item => item.ObjectId != null)
        .Select(item => new ParseCommand(
          string.Format("classes/{0}/{1}", Uri.EscapeDataString(item.ClassName), Uri.EscapeDataString(item.ObjectId)),
            method: "DELETE",
            data: null))
        .ToList();
      return ExecuteBatchRequests(requests, sessionToken, cancellationToken).Cast<Task>().ToList();
    }

    // TODO (hallucinogen): move this out to a class to be used by Analytics
    private const int MaximumBatchSize = 50;
    internal IList<Task<IDictionary<string, object>>> ExecuteBatchRequests(IList<ParseCommand> requests,
        string sessionToken,
        CancellationToken cancellationToken) {
      var tasks = new List<Task<IDictionary<string, object>>>();
      int batchSize = requests.Count;

      IEnumerable<ParseCommand> remaining = requests;
      while (batchSize > MaximumBatchSize) {
        var process = remaining.Take(MaximumBatchSize).ToList();
        remaining = remaining.Skip(MaximumBatchSize);

        tasks.AddRange(ExecuteBatchRequest(process, sessionToken, cancellationToken));

        batchSize = remaining.Count();
      }
      tasks.AddRange(ExecuteBatchRequest(remaining.ToList(), sessionToken, cancellationToken));

      return tasks;
    }

    private IList<Task<IDictionary<string, object>>> ExecuteBatchRequest(IList<ParseCommand> requests,
        string sessionToken,
        CancellationToken cancellationToken) {
      var tasks = new List<Task<IDictionary<string, object>>>();
      int batchSize = requests.Count;
      var tcss = new List<TaskCompletionSource<IDictionary<string, object>>>();
      for (int i = 0; i < batchSize; ++i) {
        var tcs = new TaskCompletionSource<IDictionary<string, object>>();
        tcss.Add(tcs);
        tasks.Add(tcs.Task);
      }

      var encodedRequests = requests.Select(r => {
        var results = new Dictionary<string, object> {
          { "method", r.Method },
          { "path", r.Uri.AbsolutePath },
        };

        if (r.DataObject != null) {
          results["body"] = r.DataObject;
        }
        return results;
      }).Cast<object>().ToList();
      var command = new ParseCommand("batch",
        method: "POST",
        sessionToken: sessionToken,
        data: new Dictionary<string, object> { { "requests", encodedRequests } });

      commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).ContinueWith(t => {
        if (t.IsFaulted || t.IsCanceled) {
          foreach (var tcs in tcss) {
            if (t.IsFaulted) {
              tcs.TrySetException(t.Exception);
            } else if (t.IsCanceled) {
              tcs.TrySetCanceled();
            }
          }
          return;
        }

        var resultsArray = Conversion.As<IList<object>>(t.Result.Item2["results"]);
        int resultLength = resultsArray.Count;
        if (resultLength != batchSize) {
          foreach (var tcs in tcss) {
            tcs.TrySetException(new InvalidOperationException(
                "Batch command result count expected: " + batchSize + " but was: " + resultLength + "."));
          }
          return;
        }

        for (int i = 0; i < batchSize; ++i) {
          var result = resultsArray[i] as Dictionary<string, object>;
          var tcs = tcss[i];

          if (result.ContainsKey("success")) {
            tcs.TrySetResult(result["success"] as IDictionary<string, object>);
          } else if (result.ContainsKey("error")) {
            var error = result["error"] as IDictionary<string, object>;
            long errorCode = (long)error["code"];
            tcs.TrySetException(new ParseException((ParseException.ErrorCode)errorCode, error["error"] as string));
          } else {
            tcs.TrySetException(new InvalidOperationException(
                "Invalid batch command response."));
          }
        }
      });

      return tasks;
    }
  }
}
