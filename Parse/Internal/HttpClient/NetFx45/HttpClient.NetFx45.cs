// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Parse.Internal {
  internal class HttpClient : IHttpClient {
    public Task<Tuple<HttpStatusCode, string>> ExecuteAsync(HttpRequest httpRequest,
        IProgress<ParseUploadProgressEventArgs> uploadProgress,
        IProgress<ParseDownloadProgressEventArgs> downloadProgress,
        CancellationToken cancellationToken) {
      HttpWebRequest request = HttpWebRequest.Create(httpRequest.Uri) as HttpWebRequest;
      request.Method = httpRequest.Method;
      cancellationToken.Register(() => request.Abort());
      uploadProgress = uploadProgress ?? new Progress<ParseUploadProgressEventArgs>();
      downloadProgress = downloadProgress ?? new Progress<ParseDownloadProgressEventArgs>();

      // Fill in zero-length data if method is post.
      Stream data = httpRequest.Data;
      if (httpRequest.Data == null && httpRequest.Method.ToLower().Equals("post")) {
        data = new MemoryStream(new byte[0]);
      }

      // Fill in the headers
      if (httpRequest.Headers != null) {
        foreach (var header in httpRequest.Headers) {
          if (header.Key == "Content-Type") {
            // Move over Content-Type header into Content.
            request.ContentType = header.Value;
          } else {
            request.Headers[header.Key] = header.Value;
          }
        }
      }
      // Avoid aggressive caching on Windows Phone 8.1.
      request.Headers["Cache-Control"] = "no-cache";

      Task uploadTask = null;

      if (data != null) {
        Task copyTask = null;
        long totalLength = -1;

        try {
          totalLength = data.Length;
        } catch (NotSupportedException) {
        }

        // If the length can't be determined, read it into memory first.
        if (totalLength == -1) {
          var memStream = new MemoryStream();
          copyTask = data.CopyToAsync(memStream).OnSuccess(_ => {
            memStream.Seek(0, SeekOrigin.Begin);
            totalLength = memStream.Length;

            data = memStream;
          });
        }

        uploadProgress.Report(new ParseUploadProgressEventArgs { Progress = 0 });

        uploadTask = copyTask.Safe().ContinueWith(_ => {
          return request.GetRequestStreamAsync();
        }).Unwrap().OnSuccess(t => {
          var requestStream = t.Result;

          int bufferSize = 4096;
          byte[] buffer = new byte[bufferSize];
          int bytesRead = 0;
          long readSoFar = 0;

          return InternalExtensions.WhileAsync(() => {
            return data.ReadAsync(buffer, 0, bufferSize, cancellationToken).OnSuccess(readTask => {
              bytesRead = readTask.Result;
              return bytesRead > 0;
            });
          }, () => {
            cancellationToken.ThrowIfCancellationRequested();
            return requestStream.WriteAsync(buffer, 0, bytesRead, cancellationToken).OnSuccess(_ => {
              cancellationToken.ThrowIfCancellationRequested();
              readSoFar += bytesRead;
              uploadProgress.Report(new ParseUploadProgressEventArgs { Progress = 1.0 * readSoFar / totalLength });
            });
          }).ContinueWith(_ => {
            //requestStream.Flush();
            requestStream.Dispose();
            return _;
          }).Unwrap();
        }).Unwrap();
      }

      return uploadTask.Safe().OnSuccess(_ => {
        return request.GetResponseAsync();
      }).Unwrap().ContinueWith(t => {
        // Handle canceled
        cancellationToken.ThrowIfCancellationRequested();

        var resultStream = new MemoryStream();
        HttpWebResponse response = null;
        if (t.IsFaulted) {
          if (t.Exception.InnerException is WebException) {
            var webException = t.Exception.InnerException as WebException;
            response = (HttpWebResponse)webException.Response;
          } else {
            TaskCompletionSource<Tuple<HttpStatusCode, string>> tcs = new TaskCompletionSource<Tuple<HttpStatusCode, string>>();
            tcs.TrySetException(t.Exception);

            return tcs.Task;
          }
        } else {
          response = (HttpWebResponse)t.Result;
        }

        var responseStream = response.GetResponseStream();

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
      }).Unwrap();
    }
  }
}
