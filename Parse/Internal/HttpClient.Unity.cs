// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Parse.Internal {
  internal class HttpClient : IHttpClient {
    public Task<Tuple<HttpStatusCode, string>> ExecuteAsync(HttpRequest httpRequest,
        IProgress<ParseUploadProgressEventArgs> uploadProgress,
        IProgress<ParseDownloadProgressEventArgs> downloadProgress,
        CancellationToken cancellationToken) {
      var tcs = new TaskCompletionSource<Tuple<HttpStatusCode, string>>();
      cancellationToken.Register(() => tcs.TrySetCanceled());
      uploadProgress = uploadProgress ?? new Progress<ParseUploadProgressEventArgs>();
      downloadProgress = downloadProgress ?? new Progress<ParseDownloadProgressEventArgs>();

      var headerTable = new Hashtable();
      // Fill in the headers
      if (httpRequest.Headers != null) {
        foreach (var pair in httpRequest.Headers) {
          headerTable[pair.Key] = pair.Value;
        }
      }

      // Explicitly assume a JSON content.
      if (!headerTable.ContainsKey("Content-Type")) {
        headerTable["Content-Type"] = "application/json";
      }

      Task readBytesTask = null;
      IDisposable toDisposeAfterReading = null;
      byte[] bytes = null;
      if (!httpRequest.Method.Equals("POST") || httpRequest.Data == null) {
        bool noBody = httpRequest.Data == null;
        Stream data = httpRequest.Data ?? new MemoryStream(UTF8Encoding.UTF8.GetBytes("{}"));
        var reader = new StreamReader(data);
        toDisposeAfterReading = reader;
        Task<string> streamReaderTask;

        if (PlatformHooks.IsCompiledByIL2CPP) {
          streamReaderTask = Task.FromResult(reader.ReadToEnd());
        } else {
          streamReaderTask = reader.ReadToEndAsync();
        }

        readBytesTask = streamReaderTask.OnSuccess(t => {
          var parsed = Json.Parse(t.Result) as IDictionary<string, object>;
          // Inject the method
          parsed["_method"] = httpRequest.Method;
          parsed["_noBody"] = noBody;
          bytes = UTF8Encoding.UTF8.GetBytes(Json.Encode(parsed));
        });
      } else {
        var ms = new MemoryStream();
        toDisposeAfterReading = ms;
        readBytesTask = httpRequest.Data.CopyToAsync(ms).OnSuccess(_ => {
          bytes = ms.ToArray();
        });
      }

      readBytesTask.Safe().ContinueWith(t => {
        if (toDisposeAfterReading != null) {
          toDisposeAfterReading.Dispose();
        }
        return t;
      }).Unwrap()
      .OnSuccess(_ => {
        float oldDownloadProgress = 0;
        float oldUploadProgress = 0;

        PlatformHooks.RunOnMainThread(() => {
          PlatformHooks.RegisterNetworkRequest(GenerateWWWInstance(httpRequest.Uri.AbsoluteUri, bytes, headerTable), www => {
            if (cancellationToken.IsCancellationRequested) {
              tcs.TrySetCanceled();
              return;
            }
            if (www.isDone) {
              uploadProgress.Report(new ParseUploadProgressEventArgs { Progress = 1 });
              downloadProgress.Report(new ParseDownloadProgressEventArgs { Progress = 1 });

              var statusCode = GetStatusCode(www);
              // Returns HTTP error if that's the only info we have.
              if (!String.IsNullOrEmpty(www.error) && String.IsNullOrEmpty(www.text)) {
                var errorString = string.Format("{{\"error\":\"{0}\"}}", www.error);
                tcs.TrySetResult(new Tuple<HttpStatusCode, string>(statusCode, errorString));
              } else {
                tcs.TrySetResult(new Tuple<HttpStatusCode, string>(statusCode, www.text));
              }
            } else {
              // Update upload progress
              var newUploadProgress = www.uploadProgress;
              if (oldUploadProgress < newUploadProgress) {
                uploadProgress.Report(new ParseUploadProgressEventArgs { Progress = newUploadProgress });
              }
              oldUploadProgress = newUploadProgress;

              // Update download progress
              var newDownloadProgress = www.progress;
              if (oldDownloadProgress < newDownloadProgress) {
                downloadProgress.Report(new ParseDownloadProgressEventArgs { Progress = newDownloadProgress });
              }
              oldDownloadProgress = newDownloadProgress;
            }
          });
        });
      });

      // Get off of the main thread for further processing.
      return tcs.Task.ContinueWith(t => {
        var dispatchTcs = new TaskCompletionSource<object>();
        // ThreadPool doesn't work well in IL2CPP environment, but Thread does!
        if (PlatformHooks.IsCompiledByIL2CPP) {
          var thread = new Thread(_ => {
            dispatchTcs.TrySetResult(null);
          });
          thread.Start();
        } else {
          ThreadPool.QueueUserWorkItem(_ => dispatchTcs.TrySetResult(null));
        }
        return dispatchTcs.Task;
      }).Unwrap()
      .ContinueWith(_ => tcs.Task).Unwrap();
    }

    /// <summary>
    /// Gets the HTTP status code from finished <code>WWW</code> request.
    /// </summary>
    /// <param name="www">The WWW object.</param>
    /// <returns>Returns 201 if there's no error. Otherwise, returns error status code.</returns>
    private static HttpStatusCode GetStatusCode(WWW www) {
      if (String.IsNullOrEmpty(www.error)) {
        return (HttpStatusCode)201;
      }
      String errorCode = Regex.Match(www.error, @"\d+").Value;
      int errorNumber = 0;
      if (!Int32.TryParse(errorCode, out errorNumber)) {
        return (HttpStatusCode)400;
      }
      return (HttpStatusCode)errorNumber;
    }

    /// <summary>
    /// Unity changes its WWW constructor at 4.5.x from using <see cref="Hashtable"/> to using
    /// <see cref="Dictionary{String, String}"/>. We need to explicitly handle that. This method
    /// generates a valid WWW instance depending on the newest WWW constructor
    /// provided by used UnityEngine assembly
    /// </summary>
    private static WWW GenerateWWWInstance(string uri, byte[] bytes, Hashtable headerTable) {
      var newHeader = new Dictionary<string, string>();
      foreach (DictionaryEntry pair in headerTable) {
        newHeader[pair.Key as string] = pair.Value as string;
      }
      return new WWW(uri, bytes, newHeader);
    }
  }
}
