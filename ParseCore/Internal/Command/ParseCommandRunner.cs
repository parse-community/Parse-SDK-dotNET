// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Parse.Common.Internal;

namespace Parse.Core.Internal {
  public class ParseCommandRunner : IParseCommandRunner {
    private readonly IHttpClient httpClient;
    private readonly IInstallationIdController installationIdController;

    public ParseCommandRunner(IHttpClient httpClient, IInstallationIdController installationIdController) {
      this.httpClient = httpClient;
      this.installationIdController = installationIdController;
    }

    public Task<Tuple<HttpStatusCode, IDictionary<string, object>>> RunCommandAsync(ParseCommand command,
        IProgress<ParseUploadProgressEventArgs> uploadProgress = null,
        IProgress<ParseDownloadProgressEventArgs> downloadProgress = null,
        CancellationToken cancellationToken = default(CancellationToken)) {
      return PrepareCommand(command).ContinueWith(commandTask => {
        return httpClient.ExecuteAsync(commandTask.Result, uploadProgress, downloadProgress, cancellationToken).OnSuccess(t => {
          cancellationToken.ThrowIfCancellationRequested();

          var response = t.Result;
          var contentString = response.Item2;
          int responseCode = (int)response.Item1;
          if (responseCode >= 500) {
            // Server error, return InternalServerError.
            throw new ParseException(ParseException.ErrorCode.InternalServerError, response.Item2);
          } else if (contentString != null) {
            IDictionary<string, object> contentJson = null;
            try {
              if (contentString.StartsWith("[")) {
                var arrayJson = Json.Parse(contentString);
                contentJson = new Dictionary<string, object> { { "results", arrayJson } };
              } else {
                contentJson = Json.Parse(contentString) as IDictionary<string, object>;
              }
            } catch (Exception e) {
              throw new ParseException(ParseException.ErrorCode.OtherCause,
                  "Invalid response from server", e);
            }
            if (responseCode < 200 || responseCode > 299) {
              int code = (int)(contentJson.ContainsKey("code") ? (long)contentJson["code"] : (int)ParseException.ErrorCode.OtherCause);
              string error = contentJson.ContainsKey("error") ?
                  contentJson["error"] as string :
                  contentString;
              throw new ParseException((ParseException.ErrorCode)code, error);
            }
            return new Tuple<HttpStatusCode, IDictionary<string, object>>(response.Item1,
                contentJson);
          }
          return new Tuple<HttpStatusCode, IDictionary<string, object>>(response.Item1, null);
        });
      }).Unwrap();
    }

    private const string revocableSessionTokenTrueValue = "1";
    private Task<ParseCommand> PrepareCommand(ParseCommand command) {
      ParseCommand newCommand = new ParseCommand(command);

      Task<ParseCommand> installationIdTask = installationIdController.GetAsync().ContinueWith(t => {
        newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-Installation-Id", t.Result.ToString()));
        return newCommand;
      });

      // TODO (richardross): Inject configuration instead of using shared static here.
      ParseClient.Configuration configuration = ParseClient.CurrentConfiguration;
      newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-Application-Id", configuration.ApplicationId));
      newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-Client-Version", ParseClient.VersionString));

      if (configuration.AdditionalHTTPHeaders != null) {
        foreach (var header in configuration.AdditionalHTTPHeaders) {
          newCommand.Headers.Add(header);
        }
      }

      if (!string.IsNullOrEmpty(configuration.VersionInfo.BuildVersion)) {
        newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-App-Build-Version", configuration.VersionInfo.BuildVersion));
      }
      if (!string.IsNullOrEmpty(configuration.VersionInfo.DisplayVersion)) {
        newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-App-Display-Version", configuration.VersionInfo.DisplayVersion));
      }
      if (!string.IsNullOrEmpty(configuration.VersionInfo.OSVersion)) {
        newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-OS-Version", configuration.VersionInfo.OSVersion));
      }

      // TODO (richardross): I hate the idea of having this super tightly coupled static variable in here.
      // Lets eventually get rid of it.
      if (!string.IsNullOrEmpty(ParseClient.MasterKey)) {
        newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-Master-Key", ParseClient.MasterKey));
      } else {
        newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-Windows-Key", configuration.WindowsKey));
      }

      // TODO (richardross): Inject this instead of using static here.
      if (ParseUser.IsRevocableSessionEnabled) {
        newCommand.Headers.Add(new KeyValuePair<string, string>("X-Parse-Revocable-Session", revocableSessionTokenTrueValue));
      }

      return installationIdTask;
    }
  }
}
