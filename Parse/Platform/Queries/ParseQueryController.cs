using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Platform.Objects;
using Parse.Abstractions.Platform.Queries;
using Parse.Infrastructure.Data;
using Parse.Infrastructure.Execution;
using Parse.Infrastructure.Utilities;

namespace Parse.Platform.Queries;

/// <summary>
/// A straightforward implementation of <see cref="IParseQueryController"/> that uses <see cref="ParseObject.Services"/> to decode raw server data when needed.
/// </summary>

internal class ParseQueryController : IParseQueryController
{
    private IParseCommandRunner CommandRunner { get; }
    private IParseDataDecoder Decoder { get; }

    public ParseQueryController(IParseCommandRunner commandRunner, IParseDataDecoder decoder)
    {
        CommandRunner = commandRunner;
        Decoder = decoder;
    }

    public async Task<IEnumerable<IObjectState>> FindAsync<T>(ParseQuery<T> query, ParseUser user, CancellationToken cancellationToken = default) where T : ParseObject
    {
        var result = await FindAsync(query.ClassName, query.BuildParameters(), user?.SessionToken, cancellationToken).ConfigureAwait(false);
        var rawResults = result["results"] as IList<object> ?? new List<object>();

        return rawResults
            .Select(item => ParseObjectCoder.Instance.Decode(item as IDictionary<string, object>, Decoder, user?.Services));
    }

    public async Task<int> CountAsync<T>(ParseQuery<T> query, ParseUser user, CancellationToken cancellationToken = default) where T : ParseObject
    {
        var parameters = query.BuildParameters();
        parameters["limit"] = 0;
        parameters["count"] = 1;

        var result = await FindAsync(query.ClassName, parameters, user?.SessionToken, cancellationToken).ConfigureAwait(false);
        return Convert.ToInt32(result["count"]);
    }

    public async Task<IObjectState> FirstAsync<T>(ParseQuery<T> query, ParseUser user, CancellationToken cancellationToken = default) where T : ParseObject
    {
        var parameters = query.BuildParameters();
        parameters["limit"] = 1;

        var result = await FindAsync(query.ClassName, parameters, user?.SessionToken, cancellationToken).ConfigureAwait(false);
        var rawResults = result["results"] as IList<object> ?? new List<object>();

        var firstItem = rawResults.FirstOrDefault() as IDictionary<string, object>;
        return firstItem != null ? ParseObjectCoder.Instance.Decode(firstItem, Decoder, user?.Services) : null;
    }

    private async Task<IDictionary<string, object>> FindAsync(string className, IDictionary<string, object> parameters, string sessionToken, CancellationToken cancellationToken = default)
    {
        var command = new ParseCommand(
            $"classes/{Uri.EscapeDataString(className)}?{ParseClient.BuildQueryString(parameters)}",
            method: "GET",
            sessionToken: sessionToken,
            data: null
        );

        var response = await CommandRunner.RunCommandAsync(command, null,null,cancellationToken).ConfigureAwait(false);
        return response.Item2;
    }
}