using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure.Utilities;
using Parse.Infrastructure;
using Parse.Abstractions.Internal;
using Parse.Infrastructure.Execution;
using Parse.Infrastructure.Data;

namespace Parse.Platform.Objects;

public class ParseObjectController : IParseObjectController
{
    IParseCommandRunner CommandRunner { get; }

    IParseDataDecoder Decoder { get; }

    IServerConnectionData ServerConnectionData { get; }

    public ParseObjectController(IParseCommandRunner commandRunner, IParseDataDecoder decoder, IServerConnectionData serverConnectionData) => (CommandRunner, Decoder, ServerConnectionData) = (commandRunner, decoder, serverConnectionData);

    public async Task<IObjectState> FetchAsync(IObjectState state, string sessionToken, IServiceHub serviceHub, CancellationToken cancellationToken = default)
    {
        var command = new ParseCommand($"classes/{Uri.EscapeDataString(state.ClassName)}/{Uri.EscapeDataString(state.ObjectId)}", method: "GET", sessionToken: sessionToken, data: null);

        var result = await CommandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).ConfigureAwait(false);
        return ParseObjectCoder.Instance.Decode(result.Item2, Decoder, serviceHub);
    }


    public async Task<IObjectState> SaveAsync(IObjectState state, IDictionary<string, IParseFieldOperation> operations, string sessionToken, IServiceHub serviceHub, CancellationToken cancellationToken = default)
    {
        ParseCommand command;
        if (state.ObjectId == null)
        {
            command = new ParseCommand($"classes/{Uri.EscapeDataString(state.ClassName)}", method: state.ObjectId == null ? "POST" : "PUT", sessionToken: sessionToken, data: serviceHub.GenerateJSONObjectForSaving(operations));
        }
        else
        {
            command = new ParseCommand($"classes/{Uri.EscapeDataString(state.ClassName)}/{state.ObjectId}", method: state.ObjectId == null ? "POST" : "PUT", sessionToken: sessionToken, data: serviceHub.GenerateJSONObjectForSaving(operations));
        }
        var result = await CommandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).ConfigureAwait(false);
        var decodedState = ParseObjectCoder.Instance.Decode(result.Item2, Decoder, serviceHub);

        // Mutating the state and marking it as new if the status code is Created
        decodedState.MutatedClone(mutableClone => mutableClone.IsNew = result.Item1 == System.Net.HttpStatusCode.Created);

        return decodedState;
    }


    public async Task<IList<Task<IObjectState>>> SaveAllAsync(IList<IObjectState> states,IList<IDictionary<string, IParseFieldOperation>> operationsList,string sessionToken,IServiceHub serviceHub,CancellationToken cancellationToken = default)
    {
        // Create a list of tasks where each task represents a command to be executed
        var tasks =
            states.Zip(operationsList, (state, operations) => new ParseCommand(state.ObjectId == null? $"classes/{Uri.EscapeDataString(state.ClassName)}": $"classes/{Uri.EscapeDataString(state.ClassName)}/{Uri.EscapeDataString(state.ObjectId)}",
            method: state.ObjectId == null ? "POST" : "PUT",sessionToken: sessionToken,data: serviceHub.GenerateJSONObjectForSaving(operations)))
        .Select(command => CommandRunner.RunCommandAsync(command,null,null, cancellationToken)) // Run commands asynchronously
        .ToList();

        // Wait for all tasks to complete
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        var decodedStates = results.Select(result => ParseObjectCoder.Instance.Decode(result.Item2, Decoder, serviceHub)).ToList();
        // Decode results and return a list of tasks that resolve to IObjectState
        return results.Select(result =>
            Task.FromResult(ParseObjectCoder.Instance.Decode(result.Item2, Decoder, serviceHub)) // Return a task that resolves to IObjectState
        ).ToList();
    }




    public Task DeleteAsync(IObjectState state, string sessionToken, CancellationToken cancellationToken = default)
    {
        return CommandRunner.RunCommandAsync(new ParseCommand($"classes/{state.ClassName}/{state.ObjectId}", method: "DELETE", sessionToken: sessionToken, data: null), cancellationToken: cancellationToken);
    }

    public IList<Task> DeleteAllAsync(IList<IObjectState> states, string sessionToken, CancellationToken cancellationToken = default)
    {
        return ExecuteBatchRequests(states.Where(item => item.ObjectId is { }).Select(item => new ParseCommand($"classes/{Uri.EscapeDataString(item.ClassName)}/{Uri.EscapeDataString(item.ObjectId)}", method: "DELETE", data: default)).ToList(), sessionToken, cancellationToken).Cast<Task>().ToList();
    }

    int MaximumBatchSize { get; } = 50;

    // TODO (hallucinogen): move this out to a class to be used by Analytics

    internal IList<Task<IDictionary<string, object>>> ExecuteBatchRequests(IList<ParseCommand> requests, string sessionToken, CancellationToken cancellationToken = default)
    {
        List<Task<IDictionary<string, object>>> tasks = new List<Task<IDictionary<string, object>>>();
        int batchSize = requests.Count;

        IEnumerable<ParseCommand> remaining = requests;

        while (batchSize > MaximumBatchSize)
        {
            List<ParseCommand> process = remaining.Take(MaximumBatchSize).ToList();

            remaining = remaining.Skip(MaximumBatchSize);
            tasks.AddRange(ExecuteBatchRequest(process, sessionToken, cancellationToken));
            batchSize = remaining.Count();
        }

        tasks.AddRange(ExecuteBatchRequest(remaining.ToList(), sessionToken, cancellationToken));
        return tasks;
    }

    IList<Task<IDictionary<string, object>>> ExecuteBatchRequest(IList<ParseCommand> requests, string sessionToken, CancellationToken cancellationToken = default)
    {
        int batchSize = requests.Count;

        List<Task<IDictionary<string, object>>> tasks = new List<Task<IDictionary<string, object>>> { };
        List<TaskCompletionSource<IDictionary<string, object>>> completionSources = new List<TaskCompletionSource<IDictionary<string, object>>> { };

        for (int i = 0; i < batchSize; ++i)
        {
            TaskCompletionSource<IDictionary<string, object>> tcs = new TaskCompletionSource<IDictionary<string, object>>();

            completionSources.Add(tcs);
            tasks.Add(tcs.Task);
        }

        List<object> encodedRequests = requests.Select(request =>
        {
            Dictionary<string, object> results = new Dictionary<string, object>
            {
                ["method"] = request.Method,
                ["path"] = request is { Path: { }, Resource: { } } ? request.Target.AbsolutePath : new Uri(new Uri(ServerConnectionData.ServerURI), request.Path).AbsolutePath,
            };

            if (request.DataObject != null)
                results["body"] = request.DataObject;

            return results;
        }).Cast<object>().ToList();

        ParseCommand command = new ParseCommand("batch", method: "POST", sessionToken: sessionToken, data: new Dictionary<string, object> { [nameof(requests)] = encodedRequests });

        CommandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).ContinueWith(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                foreach (TaskCompletionSource<IDictionary<string, object>> tcs in completionSources)
                    if (task.IsFaulted)
                        tcs.TrySetException(task.Exception);
                    else if (task.IsCanceled)
                        tcs.TrySetCanceled();

                return;
            }

            IList<object> resultsArray = Conversion.As<IList<object>>(task.Result.Item2["results"]);
            int resultLength = resultsArray.Count;

            if (resultLength != batchSize)
            {
                foreach (TaskCompletionSource<IDictionary<string, object>> completionSource in completionSources)
                    completionSource.TrySetException(new InvalidOperationException($"Batch command result count expected: {batchSize} but was: {resultLength}."));

                return;
            }

            for (int i = 0; i < batchSize; ++i)
            {
                Dictionary<string, object> result = resultsArray[i] as Dictionary<string, object>;
                TaskCompletionSource<IDictionary<string, object>> target = completionSources[i];

                if (result.ContainsKey("success"))
                    target.TrySetResult(result["success"] as IDictionary<string, object>);
                else if (result.ContainsKey("error"))
                {
                    IDictionary<string, object> error = result["error"] as IDictionary<string, object>;
                    target.TrySetException(new ParseFailureException((ParseFailureException.ErrorCode) (long) error["code"], error[nameof(error)] as string));
                }
                else
                    target.TrySetException(new InvalidOperationException("Invalid batch command response."));
            }
        });

        return tasks;
    }
}
