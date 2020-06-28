// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

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

namespace Parse.Infrastructure.Execution
{
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

        IWebClient GetWebClient() => WebClient;

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
        public Task<Tuple<HttpStatusCode, IDictionary<string, object>>> RunCommandAsync(ParseCommand command, IProgress<IDataTransferLevel> uploadProgress = null, IProgress<IDataTransferLevel> downloadProgress = null, CancellationToken cancellationToken = default) => PrepareCommand(command).ContinueWith(commandTask => GetWebClient().ExecuteAsync(commandTask.Result, uploadProgress, downloadProgress, cancellationToken).OnSuccess(task =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            Tuple<HttpStatusCode, string> response = task.Result;
            string content = response.Item2;
            int responseCode = (int) response.Item1;

            if (responseCode >= 500)
            {
                // Server error, return InternalServerError.

                throw new ParseFailureException(ParseFailureException.ErrorCode.InternalServerError, response.Item2);
            }
            else if (content is { })
            {
                IDictionary<string, object> contentJson = default;

                try
                {
                    // TODO: Newer versions of Parse Server send the failure results back as HTML.

                    contentJson = content.StartsWith("[") ? new Dictionary<string, object> { ["results"] = JsonUtilities.Parse(content) } : JsonUtilities.Parse(content) as IDictionary<string, object>;
                }
                catch (Exception e)
                {
                    throw new ParseFailureException(ParseFailureException.ErrorCode.OtherCause, "Invalid or alternatively-formatted response recieved from server.", e);
                }

                if (responseCode < 200 || responseCode > 299)
                {
                    throw new ParseFailureException(contentJson.ContainsKey("code") ? (ParseFailureException.ErrorCode) (long) contentJson["code"] : ParseFailureException.ErrorCode.OtherCause, contentJson.ContainsKey("error") ? contentJson["error"] as string : content);
                }

                return new Tuple<HttpStatusCode, IDictionary<string, object>>(response.Item1, contentJson);
            }
            return new Tuple<HttpStatusCode, IDictionary<string, object>>(response.Item1, null);
        })).Unwrap();

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
}
