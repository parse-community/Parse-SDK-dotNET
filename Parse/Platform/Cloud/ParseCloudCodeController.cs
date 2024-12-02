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

namespace Parse.Platform.Cloud;

public class ParseCloudCodeController : IParseCloudCodeController
{
    IParseCommandRunner CommandRunner { get; }

    IParseDataDecoder Decoder { get; }

    public ParseCloudCodeController(IParseCommandRunner commandRunner, IParseDataDecoder decoder) => (CommandRunner, Decoder) = (commandRunner, decoder);

    public async Task<T> CallFunctionAsync<T>(string name, IDictionary<string, object> parameters, string sessionToken, IServiceHub serviceHub, CancellationToken cancellationToken = default)
    {
        // Run the command asynchronously and await the result
        var commandResult = await CommandRunner.RunCommandAsync(
            new ParseCommand($"functions/{Uri.EscapeUriString(name)}", method: "POST", sessionToken: sessionToken,
            data: NoObjectsEncoder.Instance.Encode(parameters, serviceHub) as IDictionary<string, object>),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        // Decode the result and handle it
        var decoded = Decoder.Decode(commandResult.Item2, serviceHub) as IDictionary<string, object>;

        // Return the decoded result or the default value if not found
        return decoded?.ContainsKey("result") == true
            ? Conversion.To<T>(decoded["result"])
            : default;
    }

}
