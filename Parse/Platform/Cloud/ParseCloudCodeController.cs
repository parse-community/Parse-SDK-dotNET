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
using Parse.Infrastructure;
using System.Diagnostics;

namespace Parse.Platform.Cloud;

public class ParseCloudCodeController : IParseCloudCodeController
{
    IParseCommandRunner CommandRunner { get; }
    IParseDataDecoder Decoder { get; }

    public ParseCloudCodeController(IParseCommandRunner commandRunner, IParseDataDecoder decoder) =>
        (CommandRunner, Decoder) = (commandRunner, decoder);
    public async Task<T> CallFunctionAsync<T>(
    string name,
    IDictionary<string, object> parameters,
    string sessionToken,
    IServiceHub serviceHub,
    CancellationToken cancellationToken = default,
    IProgress<IDataTransferLevel> uploadProgress = null,
    IProgress<IDataTransferLevel> downloadProgress = null)
    {
        try
        {
            // Prepare the command
            var command = new ParseCommand(
                $"functions/{Uri.EscapeUriString(name)}",
                method: "POST",
                sessionToken: sessionToken,
                data: NoObjectsEncoder.Instance.Encode(parameters, serviceHub) as IDictionary<string, object>);

            // Execute the command with progress tracking
            var commandResult = await CommandRunner.RunCommandAsync(
                command,
                uploadProgress,
                downloadProgress,
                cancellationToken).ConfigureAwait(false);

            // Ensure the command result is valid
            if (commandResult.Item2 == null)
            {
                throw new ParseFailureException(ParseFailureException.ErrorCode.OtherCause, "Cloud function returned no data.");
            }

            // Decode the result
            var decoded = Decoder.Decode(commandResult.Item2, serviceHub) as IDictionary<string, object>;

            // Extract the result key
            if (decoded.TryGetValue("result", out var result))
            {
                try
                {
                    return Conversion.To<T>(result);
                }
                catch (Exception ex)
                {
                    throw new ParseFailureException(ParseFailureException.ErrorCode.OtherCause, "Failed to convert cloud function result to expected type.", ex);
                }
            }


            // Handle missing result key
            throw new ParseFailureException(ParseFailureException.ErrorCode.OtherCause, "Cloud function did not return a result.");
        }
        catch (ParseFailureException)
        {
            // Rethrow known Parse exceptions
            throw;
        }
    }

}

