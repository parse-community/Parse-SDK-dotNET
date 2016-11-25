// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Utilities;
using LeanCloud.Common.Internal;

namespace LeanCloud.Core.Internal {
  public class AVObjectController : IAVObjectController {
    private readonly IAVCommandRunner commandRunner;

    public AVObjectController(IAVCommandRunner commandRunner) {
      this.commandRunner = commandRunner;
    }

    public Task<IObjectState> FetchAsync(IObjectState state,
        string sessionToken,
        CancellationToken cancellationToken) {
      var command = new AVCommand(string.Format("classes/{0}/{1}",
              Uri.EscapeDataString(state.ClassName),
              Uri.EscapeDataString(state.ObjectId)),
          method: "GET",
          sessionToken: sessionToken,
          data: null);

      return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t => {
        return AVObjectCoder.Instance.Decode(t.Result.Item2, AVDecoder.Instance);
      });
    }

    public Task<IObjectState> SaveAsync(IObjectState state,
        IDictionary<string, IAVFieldOperation> operations,
        string sessionToken,
        CancellationToken cancellationToken) {
      var objectJSON = AVObject.ToJSONObjectForSaving(operations);

      var command = new AVCommand((state.ObjectId == null ?
              string.Format("classes/{0}", Uri.EscapeDataString(state.ClassName)) :
              string.Format("classes/{0}/{1}", Uri.EscapeDataString(state.ClassName), state.ObjectId)),
          method: (state.ObjectId == null ? "POST" : "PUT"),
          sessionToken: sessionToken,
          data: objectJSON);

      return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(t => {
        var serverState = AVObjectCoder.Instance.Decode(t.Result.Item2, AVDecoder.Instance);
        serverState = serverState.MutatedClone(mutableClone => {
          mutableClone.IsNew = t.Result.Item1 == System.Net.HttpStatusCode.Created;
        });
        return serverState;
      });
    }

    public IList<Task<IObjectState>> SaveAllAsync(IList<IObjectState> states,
        IList<IDictionary<string, IAVFieldOperation>> operationsList,
        string sessionToken,
        CancellationToken cancellationToken) {

      var requests = states
        .Zip(operationsList, (item, ops) => new AVCommand(
          item.ObjectId == null
            ? string.Format("classes/{0}", Uri.EscapeDataString(item.ClassName))
            : string.Format("classes/{0}/{1}", Uri.EscapeDataString(item.ClassName), Uri.EscapeDataString(item.ObjectId)),
          method: item.ObjectId == null ? "POST" : "PUT",
          data: AVObject.ToJSONObjectForSaving(ops)))
        .ToList();

      var batchTasks = ExecuteBatchRequests(requests, sessionToken, cancellationToken);
      var stateTasks = new List<Task<IObjectState>>();
      foreach (var task in batchTasks) {
        stateTasks.Add(task.OnSuccess(t => {
          return AVObjectCoder.Instance.Decode(t.Result, AVDecoder.Instance);
        }));
      }

      return stateTasks;
    }

    public Task DeleteAsync(IObjectState state,
        string sessionToken,
        CancellationToken cancellationToken) {
      var command = new AVCommand(string.Format("classes/{0}/{1}",
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
        .Select(item => new AVCommand(
          string.Format("classes/{0}/{1}", Uri.EscapeDataString(item.ClassName), Uri.EscapeDataString(item.ObjectId)),
            method: "DELETE",
            data: null))
        .ToList();
      return ExecuteBatchRequests(requests, sessionToken, cancellationToken).Cast<Task>().ToList();
    }

    // TODO (hallucinogen): move this out to a class to be used by Analytics
    private const int MaximumBatchSize = 50;
    internal IList<Task<IDictionary<string, object>>> ExecuteBatchRequests(IList<AVCommand> requests,
        string sessionToken,
        CancellationToken cancellationToken) {
      var tasks = new List<Task<IDictionary<string, object>>>();
      int batchSize = requests.Count;

      IEnumerable<AVCommand> remaining = requests;
      while (batchSize > MaximumBatchSize) {
        var process = remaining.Take(MaximumBatchSize).ToList();
        remaining = remaining.Skip(MaximumBatchSize);

        tasks.AddRange(ExecuteBatchRequest(process, sessionToken, cancellationToken));

        batchSize = remaining.Count();
      }
      tasks.AddRange(ExecuteBatchRequest(remaining.ToList(), sessionToken, cancellationToken));

      return tasks;
    }

    private IList<Task<IDictionary<string, object>>> ExecuteBatchRequest(IList<AVCommand> requests,
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
      var command = new AVCommand("batch",
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
            tcs.TrySetException(new AVException((AVException.ErrorCode)errorCode, error["error"] as string));
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
