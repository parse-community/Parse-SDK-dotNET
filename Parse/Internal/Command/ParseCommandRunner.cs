// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;


namespace Parse.Internal {
  internal class ParseCommandRunner : IParseCommandRunner {
    private readonly IHttpClient httpClient;
    public ParseCommandRunner(IHttpClient httpClient) {
      this.httpClient = httpClient;
    }

    public Task<Tuple<HttpStatusCode, IDictionary<string, object>>> RunCommandAsync(ParseCommand command,
        IProgress<ParseUploadProgressEventArgs> uploadProgress = null,
        IProgress<ParseDownloadProgressEventArgs> downloadProgress = null,
        CancellationToken cancellationToken = default(CancellationToken)) {
      return httpClient.ExecuteAsync(command, uploadProgress, downloadProgress, cancellationToken).OnSuccess(t => {
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
    }
  }
}
