// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;

using NetHttpClient = System.Net.Http.HttpClient;
using System.Net.Http.Headers;
using System.Collections.Generic;

namespace Parse.Common.Internal {
  public class HttpClient : IHttpClient {
    private static HashSet<string> HttpContentHeaders = new HashSet<string> {
      { "Allow" },
      { "Content-Disposition" },
      { "Content-Encoding" },
      { "Content-Language" },
      { "Content-Length" },
      { "Content-Location" },
      { "Content-MD5" },
      { "Content-Range" },
      { "Content-Type" },
      { "Expires" },
      { "Last-Modified" }
    };

    public HttpClient(): this(new NetHttpClient()) {
    }

    public HttpClient(NetHttpClient client) {
      this.client = client;
    }

    private NetHttpClient client;

    public Task<Tuple<HttpStatusCode, string>> ExecuteAsync(HttpRequest httpRequest,
        IProgress<ParseUploadProgressEventArgs> uploadProgress,
        IProgress<ParseDownloadProgressEventArgs> downloadProgress,
        CancellationToken cancellationToken) {
      uploadProgress = uploadProgress ?? new Progress<ParseUploadProgressEventArgs>();
      downloadProgress = downloadProgress ?? new Progress<ParseDownloadProgressEventArgs>();

      HttpMethod httpMethod = new HttpMethod(httpRequest.Method);
      HttpRequestMessage message = new HttpRequestMessage(httpMethod, httpRequest.Uri);

      // Fill in zero-length data if method is post.
      Stream data = httpRequest.Data;
      if (httpRequest.Data == null && httpRequest.Method.ToLower().Equals("post")) {
        data = new MemoryStream(new byte[0]);
      }

      if (data != null) {
        message.Content = new StreamContent(data);
      }

      if (httpRequest.Headers != null) {
        foreach (var header in httpRequest.Headers) {
          if (HttpContentHeaders.Contains(header.Key)) {
            message.Content.Headers.Add(header.Key, header.Value);
          } else {
            message.Headers.Add(header.Key, header.Value);
          }
        }
      }

      // Avoid aggressive caching on Windows Phone 8.1.
      message.Headers.Add("Cache-Control", "no-cache");
      message.Headers.IfModifiedSince = DateTimeOffset.UtcNow;

      // TODO: (richardross) investigate progress here, maybe there's something we're missing in order to support this.
      uploadProgress.Report(new ParseUploadProgressEventArgs { Progress = 0 });

      return client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
        .ContinueWith(httpMessageTask => {
          var response = httpMessageTask.Result;

          uploadProgress.Report(new ParseUploadProgressEventArgs { Progress = 1 });

          return response.Content.ReadAsStreamAsync().ContinueWith(streamTask => {
            var resultStream = new MemoryStream();
            var responseStream = streamTask.Result;

            int bufferSize = 4096;
            byte[] buffer = new byte[bufferSize];
            int bytesRead = 0;
            long totalLength = -1;
            long readSoFar = 0;

            try {
              totalLength = responseStream.Length;
            } catch (NotSupportedException) {
            }

            return InternalExtensions.WhileAsync(() => {
              return responseStream.ReadAsync(buffer, 0, bufferSize, cancellationToken).OnSuccess(readTask => {
                bytesRead = readTask.Result;
                return bytesRead > 0;
              });
            }, () => {
              cancellationToken.ThrowIfCancellationRequested();

              return resultStream.WriteAsync(buffer, 0, bytesRead, cancellationToken).OnSuccess(_ => {
                cancellationToken.ThrowIfCancellationRequested();
                readSoFar += bytesRead;

                if (totalLength > -1) {
                  downloadProgress.Report(new ParseDownloadProgressEventArgs { Progress = 1.0 * readSoFar / totalLength });
                }
              });
            }).ContinueWith(_ => {
              responseStream.Dispose();
              return _;
            }).Unwrap().OnSuccess(_ => {
              // If getting stream size is not supported, then report download only once.
              if (totalLength == -1) {
                downloadProgress.Report(new ParseDownloadProgressEventArgs { Progress = 1.0 });
              }

              // Assume UTF-8 encoding.
              var resultAsArray = resultStream.ToArray();
              var resultString = Encoding.UTF8.GetString(resultAsArray, 0, resultAsArray.Length);
              resultStream.Dispose();
              return new Tuple<HttpStatusCode, string>(response.StatusCode, resultString);
            });
          });
        }).Unwrap().Unwrap();
    }
  }
}
