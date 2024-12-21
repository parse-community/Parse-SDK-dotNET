using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Platform.Installations;
using Parse.Abstractions.Platform.Users;
using Parse.Infrastructure.Utilities;

namespace Parse.Infrastructure.Execution;

/// <summary>
/// The command runner for all SDK operations that need to interact with the targeted deployment of Parse Server.
/// </summary>
public class ParseCommandRunner : IParseCommandRunner
{
    IWebClient WebClient { get; }

    IParseInstallationController InstallationController { get; }

    IMetadataController MetadataController { get; }

    IServerConnectionData ServerConnectionData { get; }

    Lazy<IParseUserController> UserController { get; }

    IWebClient GetWebClient()
    {
        return WebClient;
    }

    /// <summary>
    /// Creates a new Parse SDK command runner.
    /// </summary>
    /// <param name="webClient">The <see cref="IWebClient"/> implementation instance to use.</param>
    /// <param name="installationController">The <see cref="IParseInstallationController"/> implementation instance to use.</param>
    public ParseCommandRunner(IWebClient webClient, IParseInstallationController installationController, IMetadataController metadataController, IServerConnectionData serverConnectionData, Lazy<IParseUserController> userController)
    {
        WebClient = webClient;
        InstallationController = installationController;
        MetadataController = metadataController;
        ServerConnectionData = serverConnectionData;
        UserController = userController;
    }
    /// <summary>
    /// Runs a specified <see cref="ParseCommand"/>.
    /// </summary>
    /// <param name="command">The <see cref="ParseCommand"/> to run.</param>
    /// <param name="uploadProgress">An <see cref="IProgress{ParseUploadProgressEventArgs}"/> instance to push upload progress data to.</param>
    /// <param name="downloadProgress">An <see cref="IProgress{ParseDownloadProgressEventArgs}"/> instance to push download progress data to.</param>
    /// <param name="cancellationToken">An asynchronous operation cancellation token that dictates if and when the operation should be cancelled.</param>
    /// <returns></returns>
    public async Task<Tuple<HttpStatusCode, IDictionary<string, object>>> RunCommandAsync(
        ParseCommand command,
        IProgress<IDataTransferLevel> uploadProgress = null,
        IProgress<IDataTransferLevel> downloadProgress = null,
        CancellationToken cancellationToken = default)
    {
        // Prepare the command
        var preparedCommand = await PrepareCommand(command).ConfigureAwait(false);

        // Execute the command
        var response = await GetWebClient()
            .ExecuteAsync(preparedCommand, uploadProgress, downloadProgress, cancellationToken)
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        IDictionary<string, object> contentJson = null;
        // Extract response
        var statusCode = response.Item1;
        var content = response.Item2;
        var responseCode = (int) statusCode;


        if (responseCode == 200)
        {

        }
        else if (responseCode == 404)
        {
            throw new ParseFailureException(ParseFailureException.ErrorCode.ERROR404, "Error 404");
        }
        if (responseCode == 410)
        {
            return new Tuple<HttpStatusCode, IDictionary<string, object>>(
                HttpStatusCode.Gone,
                new Dictionary<string, object>
                {
                    { "error", "Page is no longer valid" }
                }
            );
        }
        if (responseCode >= 500)
        {
            // Server error, return InternalServerError
            throw new ParseFailureException(ParseFailureException.ErrorCode.InternalServerError, content);
        }
        if (string.IsNullOrEmpty(content))
        {
            return new Tuple<HttpStatusCode, IDictionary<string, object>>(statusCode, null);
        }
        //else if(responseCode == )

        // Try to parse the content
        try
        {
            contentJson = content.StartsWith("[")
                ? new Dictionary<string, object> { ["results"] = JsonUtilities.Parse(content) }
                : JsonUtilities.Parse(content) as IDictionary<string, object>;

            // Add className if "username" exists
            if (contentJson?.ContainsKey("username") == true)
            {
                contentJson["className"] = "_User";
            }
        }
        catch (Exception ex)
        {
            return new Tuple<HttpStatusCode, IDictionary<string, object>>(
                HttpStatusCode.BadRequest,
                new Dictionary<string, object>
                {
            { "error", "Invalid or alternatively-formatted response received from server." },
            { "exception", ex.Message }
                }
            );
        }


        // Return successful response
        return new Tuple<HttpStatusCode, IDictionary<string, object>>(statusCode, contentJson);
    }

    async Task<ParseCommand> PrepareCommand(ParseCommand command)
    {
        ParseCommand newCommand = new ParseCommand(command)
        {
            Resource = ServerConnectionData.ServerURI
        };

        // Fetch Installation ID and add it to the headers
        var installationId = await InstallationController.GetAsync();
        lock (newCommand.Headers)
        {
            newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-Installation-Id", installationId.ToString()));
        }

        // Add application-specific headers
        lock (newCommand.Headers)
        {
            newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-Application-Id", ServerConnectionData.ApplicationID));
            newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-Client-Version", ParseClient.Version.ToString()));

            // Add custom headers if available
            if (ServerConnectionData.Headers != null)
            {
                foreach (KeyValuePair<string, string> header in ServerConnectionData.Headers)
                {
                    newCommand.Headers.Add(header);
                }
            }

            // Add versioning headers if metadata is available
            if (!string.IsNullOrEmpty(MetadataController.HostManifestData.Version))
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-App-Build-Version", MetadataController.HostManifestData.Version));
            }

            if (!string.IsNullOrEmpty(MetadataController.HostManifestData.ShortVersion))
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-App-Display-Version", MetadataController.HostManifestData.ShortVersion));
            }

            if (!string.IsNullOrEmpty(MetadataController.EnvironmentData.OSVersion))
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-OS-Version", MetadataController.EnvironmentData.OSVersion));
            }

            // Add master key or windows key
            if (!string.IsNullOrEmpty(ServerConnectionData.MasterKey))
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-Master-Key", ServerConnectionData.MasterKey));
            }
            else
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-Windows-Key", ServerConnectionData.Key));
            }

            // Add revocable session header if enabled
            if (UserController.Value.RevocableSessionEnabled)
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-Revocable-Session", "1"));
            }
        }

        return newCommand;

        //by the way, The original installationFetchTask variable was removed, as the async/await pattern eliminates the need for it.
    }

}
