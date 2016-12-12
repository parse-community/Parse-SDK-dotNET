// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Storage.Internal;

namespace LeanCloud.Core.Internal
{
    /// <summary>
    /// Command Runner.
    /// </summary>
    public class AVCommandRunner : IAVCommandRunner
    {
        private readonly IHttpClient httpClient;
        private readonly IInstallationIdController installationIdController;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="installationIdController"></param>
        public AVCommandRunner(IHttpClient httpClient, IInstallationIdController installationIdController)
        {
            this.httpClient = httpClient;
            this.installationIdController = installationIdController;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <param name="uploadProgress"></param>
        /// <param name="downloadProgress"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<Tuple<HttpStatusCode, IDictionary<string, object>>> RunCommandAsync(AVCommand command,
            IProgress<AVUploadProgressEventArgs> uploadProgress = null,
            IProgress<AVDownloadProgressEventArgs> downloadProgress = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return PrepareCommand(command).ContinueWith(commandTask =>
            {
                return httpClient.ExecuteAsync(commandTask.Result, uploadProgress, downloadProgress, cancellationToken).OnSuccess(t =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var response = t.Result;
                    var contentString = response.Item2;
                    int responseCode = (int)response.Item1;
                    if (responseCode >= 500)
                    {
                        // Server error, return InternalServerError.
                        throw new AVException(AVException.ErrorCode.InternalServerError, response.Item2);
                    }
                    else if (contentString != null)
                    {
                        IDictionary<string, object> contentJson = null;
                        try
                        {
                            if (contentString.StartsWith("["))
                            {
                                var arrayJson = Json.Parse(contentString);
                                contentJson = new Dictionary<string, object> { { "results", arrayJson } };
                            }
                            else
                            {
                                contentJson = Json.Parse(contentString) as IDictionary<string, object>;
                            }
                        }
                        catch (Exception e)
                        {
                            throw new AVException(AVException.ErrorCode.OtherCause,
                                "Invalid response from server", e);
                        }
                        if (responseCode < 200 || responseCode > 299)
                        {
                            int code = (int)(contentJson.ContainsKey("code") ? (long)contentJson["code"] : (int)AVException.ErrorCode.OtherCause);
                            string error = contentJson.ContainsKey("error") ?
                                contentJson["error"] as string :
                                contentString;
                            throw new AVException((AVException.ErrorCode)code, error);
                        }
                        return new Tuple<HttpStatusCode, IDictionary<string, object>>(response.Item1,
                            contentJson);
                    }
                    return new Tuple<HttpStatusCode, IDictionary<string, object>>(response.Item1, null);
                });
            }).Unwrap();
        }

        private const string revocableSessionTokenTrueValue = "1";
        private Task<AVCommand> PrepareCommand(AVCommand command)
        {
            AVCommand newCommand = new AVCommand(command);

            Task<AVCommand> installationIdTask = installationIdController.GetAsync().ContinueWith(t =>
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-LC-Installation-Id", t.Result.ToString()));
                return newCommand;
            });

            // TODO (richardross): Inject configuration instead of using shared static here.
            AVClient.Configuration configuration = AVClient.CurrentConfiguration;
            newCommand.Headers.Add(new KeyValuePair<string, string>("X-LC-Id", configuration.ApplicationId));
            newCommand.Headers.Add(new KeyValuePair<string, string>("X-LC-Client-Version", AVClient.VersionString));

            if (configuration.AdditionalHTTPHeaders != null)
            {
                foreach (var header in configuration.AdditionalHTTPHeaders)
                {
                    newCommand.Headers.Add(header);
                }
            }

            if (!string.IsNullOrEmpty(configuration.VersionInfo.BuildVersion))
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-LC-App-Build-Version", configuration.VersionInfo.BuildVersion));
            }
            if (!string.IsNullOrEmpty(configuration.VersionInfo.DisplayVersion))
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-LC-App-Display-Version", configuration.VersionInfo.DisplayVersion));
            }
            if (!string.IsNullOrEmpty(configuration.VersionInfo.OSVersion))
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-LC-OS-Version", configuration.VersionInfo.OSVersion));
            }

            // TODO (richardross): I hate the idea of having this super tightly coupled static variable in here.
            // Lets eventually get rid of it.
            if (!string.IsNullOrEmpty(AVClient.MasterKey))
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-LC-Key", AVClient.MasterKey + ",master"));
            }
            else
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-LC-Key", configuration.ApplicationKey));
            }

            // TODO (richardross): Inject this instead of using static here.
            if (AVUser.IsRevocableSessionEnabled)
            {
                newCommand.Headers.Add(new KeyValuePair<string, string>("X-LeanCloud-Revocable-Session", revocableSessionTokenTrueValue));
            }

            return installationIdTask;
        }
    }
}
