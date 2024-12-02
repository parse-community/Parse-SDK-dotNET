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

        // Extract response
        var statusCode = response.Item1;
        var content = response.Item2;
        var responseCode = (int) statusCode;

        if (responseCode >= 500)
        {
            // Server error, return InternalServerError
            throw new ParseFailureException(ParseFailureException.ErrorCode.InternalServerError, content);
        }
        else if(responseCode == 201)
        {

        }
        else if(responseCode == 404)
        {
            throw new ParseFailureException(ParseFailureException.ErrorCode.ERROR404, "Error 404");
        }
        if (string.IsNullOrEmpty(content))
        {
            return new Tuple<HttpStatusCode, IDictionary<string, object>>(statusCode, null);
        }

        // Try to parse the content
        IDictionary<string, object> contentJson = null;
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

        // Check if response status code is outside the success range
        if (responseCode < 200 || responseCode > 299)
        {
            return new Tuple<HttpStatusCode, IDictionary<string, object>>(
                (HttpStatusCode) (contentJson?.ContainsKey("code") == true ? (int) (long) contentJson["code"] : 400),
                new Dictionary<string, object>
                {
            { "error", contentJson?.ContainsKey("error") == true ? contentJson["error"] as string : content },
            { "code", contentJson?.ContainsKey("code") == true ? contentJson["code"] : null }
                }
            );
        }

        // Return successful response
        return new Tuple<HttpStatusCode, IDictionary<string, object>>(statusCode, contentJson);
    }

    Task<ParseCommand> PrepareCommand(ParseCommand command)
    {
        ParseCommand newCommand = new ParseCommand(command)
        {
            Resource = ServerConnectionData.ServerURI
        };

        Task<ParseCommand> installationIdFetchTask = InstallationController.GetAsync().ContinueWith(task =>
        {
            lock (newCommand.Headers)
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-Installation-Id", task.Result.ToString()));
            }

            return newCommand;
        });

        // Locks needed due to installationFetchTask continuation newCommand.Headers.Add-call-related race condition (occurred once in Unity).
        // TODO: Consider removal of installationFetchTask variable.

        lock (newCommand.Headers)
        {
            newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-Application-Id", ServerConnectionData.ApplicationID));
            newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-Client-Version", ParseClient.Version.ToString()));

            if (ServerConnectionData.Headers != null)
            {
                foreach (KeyValuePair<string, string> header in ServerConnectionData.Headers)
                {
                    newCommand.Headers.Add(header);
                }
            }

            if (!String.IsNullOrEmpty(MetadataController.HostManifestData.Version))
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-App-Build-Version", MetadataController.HostManifestData.Version));
            }

            if (!String.IsNullOrEmpty(MetadataController.HostManifestData.ShortVersion))
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-App-Display-Version", MetadataController.HostManifestData.ShortVersion));
            }

            if (!String.IsNullOrEmpty(MetadataController.EnvironmentData.OSVersion))
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-OS-Version", MetadataController.EnvironmentData.OSVersion));
            }

            if (!String.IsNullOrEmpty(ServerConnectionData.MasterKey))
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-Master-Key", ServerConnectionData.MasterKey));
            }
            else
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-Windows-Key", ServerConnectionData.Key));
            }

            if (UserController.Value.RevocableSessionEnabled)
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-Revocable-Session", "1"));
            }
        }

        return installationIdFetchTask;
    }
}
