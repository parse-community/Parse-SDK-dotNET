// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Parse.Common.Internal;

namespace Parse.Core.Internal
{
    /// <summary>
    /// The command runner for all SDK operations that need to interact with the targeted deployment of Parse Server.
    /// </summary>
    public class ParseCommandRunner : IParseCommandRunner
    {
        private readonly IHttpClient httpClient;
        private readonly IInstallationIdController installationIdController;

        /// <summary>
        /// Creates a new Parse SDK command runner.
        /// </summary>
        /// <param name="httpClient">The <see cref="IHttpClient"/> implementation instance to use.</param>
        /// <param name="installationIdController">The <see cref="IInstallationIdController"/> implementation instance to use.</param>
        public ParseCommandRunner(IHttpClient httpClient, IInstallationIdController installationIdController)
        {
            this.httpClient = httpClient;
            this.installationIdController = installationIdController;
        }

        /// <summary>
        /// Runs a specified <see cref="ParseCommand"/>.
        /// </summary>
        /// <param name="command">The <see cref="ParseCommand"/> to run.</param>
        /// <param name="uploadProgress">An <see cref="IProgress{ParseUploadProgressEventArgs}"/> instance to push upload progress data to.</param>
        /// <param name="downloadProgress">An <see cref="IProgress{ParseDownloadProgressEventArgs}"/> instance to push download progress data to.</param>
        /// <param name="cancellationToken">An asynchronous operation cancellation token that dictates if and when the operation should be cancelled.</param>
        /// <returns></returns>
        public Task<Tuple<HttpStatusCode, IDictionary<string, object>>> RunCommandAsync(ParseCommand command, IProgress<ParseUploadProgressEventArgs> uploadProgress = null, IProgress<ParseDownloadProgressEventArgs> downloadProgress = null, CancellationToken cancellationToken = default)
        {
            return PrepareCommand(command).ContinueWith(commandTask =>
            {
                return httpClient.ExecuteAsync(commandTask.Result, uploadProgress, downloadProgress, cancellationToken).OnSuccess(t =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Tuple<HttpStatusCode, string> response = t.Result;
                    string contentString = response.Item2;
                    int responseCode = (int) response.Item1;
                    if (responseCode >= 500)
                    {
                        // Server error, return InternalServerError.
                        throw new ParseException(ParseException.ErrorCode.InternalServerError, response.Item2);
                    }
                    else if (contentString != null)
                    {
                        IDictionary<string, object> contentJson = null;
                        try
                        {
                            // TODO: Newer versions of Parse Server send the failure results back as HTML.
                            contentJson = contentString.StartsWith("[")
                                ? new Dictionary<string, object> { ["results"] = Json.Parse(contentString) }
                                : Json.Parse(contentString) as IDictionary<string, object>;
                        }
                        catch (Exception e)
                        {
                            throw new ParseException(ParseException.ErrorCode.OtherCause, "Invalid or alternatively-formatted response recieved from server.", e);
                        }
                        if (responseCode < 200 || responseCode > 299)
                        {
                            int code = (int) (contentJson.ContainsKey("code") ? (long) contentJson["code"] : (int) ParseException.ErrorCode.OtherCause);
                            string error = contentJson.ContainsKey("error") ?
                                contentJson["error"] as string :
                                contentString;
                            throw new ParseException((ParseException.ErrorCode) code, error);
                        }
                        return new Tuple<HttpStatusCode, IDictionary<string, object>>(response.Item1, contentJson);
                    }
                    return new Tuple<HttpStatusCode, IDictionary<string, object>>(response.Item1, null);
                });
            }).Unwrap();
        }

        private const string revocableSessionTokentrueValue = "1";
        private Task<ParseCommand> PrepareCommand(ParseCommand command)
        {
            ParseCommand newCommand = new ParseCommand(command);

            Task<ParseCommand> installationIdTask = installationIdController.GetAsync().ContinueWith(t =>
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-Installation-Id", t.Result.ToString()));
                return newCommand;
            });

            // TODO (richardross): Inject configuration instead of using shared static here.
            ParseClient.Configuration configuration = ParseClient.CurrentConfiguration;
            newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-Application-Id", configuration.ApplicationID));
            newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-Client-Version", ParseClient.VersionString));

            if (configuration.AuxiliaryHeaders != null)
            {
                foreach (KeyValuePair<string, string> header in configuration.AuxiliaryHeaders)
                {
                    newCommand.Headers.Add(header);
                }
            }

            if (!String.IsNullOrEmpty(configuration.VersionInfo.BuildVersion))
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-App-Build-Version", configuration.VersionInfo.BuildVersion));
            }
            if (!String.IsNullOrEmpty(configuration.VersionInfo.DisplayVersion))
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-App-Display-Version", configuration.VersionInfo.DisplayVersion));
            }
            if (!String.IsNullOrEmpty(configuration.VersionInfo.OSVersion))
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-OS-Version", configuration.VersionInfo.OSVersion));
            }

            // TODO (richardross): I hate the idea of having this super tightly coupled static variable in here.
            // Lets eventually get rid of it.
            if (!String.IsNullOrEmpty(ParseClient.MasterKey))
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-Master-Key", ParseClient.MasterKey));
            }
            else
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-Windows-Key", configuration.Key));
            }

            // TODO (richardross): Inject this instead of using static here.
            if (ParseUser.IsRevocableSessionEnabled)
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-Revocable-Session", revocableSessionTokentrueValue));
            }

            return installationIdTask;
        }
    }
}
