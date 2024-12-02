using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Platform.Files;
using Parse.Infrastructure.Execution;
using Parse.Infrastructure.Utilities;

namespace Parse.Platform.Files;

public class ParseFileController : IParseFileController
{
    private IParseCommandRunner CommandRunner { get; }

    public ParseFileController(IParseCommandRunner commandRunner) => CommandRunner = commandRunner;

    public async Task<FileState> SaveAsync(FileState state, Stream dataStream, string sessionToken, IProgress<IDataTransferLevel> progress, CancellationToken cancellationToken = default)
    {
        // If the file is already uploaded, no need to re-upload.
        if (state.Location != null)
            return state;

        if (cancellationToken.IsCancellationRequested)
            return await Task.FromCanceled<FileState>(cancellationToken);

        long oldPosition = dataStream.Position;

        try
        {
            // Execute the file upload command
            var result = await CommandRunner.RunCommandAsync(
                new ParseCommand($"files/{state.Name}", method: "POST", sessionToken: sessionToken, contentType: state.MediaType, stream: dataStream),
                uploadProgress: progress,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            // Extract the result
            var jsonData = result.Item2;

            // Ensure the cancellation token hasn't been triggered during processing
            cancellationToken.ThrowIfCancellationRequested();

            return new FileState
            {
                Name = jsonData["name"] as string,
                Location = new Uri(jsonData["url"] as string, UriKind.Absolute),
                MediaType = state.MediaType
            };
        }
        catch (OperationCanceledException)
        {
            // Handle the cancellation properly, resetting the stream if it can seek
            if (dataStream.CanSeek)
                dataStream.Seek(oldPosition, SeekOrigin.Begin);

            throw; // Re-throw to allow the caller to handle the cancellation
        }
        catch (Exception)
        {
            // If an error occurs, reset the stream position and rethrow
            if (dataStream.CanSeek)
                dataStream.Seek(oldPosition, SeekOrigin.Begin);

            throw; // Re-throw to allow the caller to handle the error
        }
    }
}
