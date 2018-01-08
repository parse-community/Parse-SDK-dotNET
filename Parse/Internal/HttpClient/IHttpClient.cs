// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Parse.Common.Internal {
  public interface IHttpClient {
    /// <summary>
    /// Executes HTTP request to a <see cref="HttpRequest.Uri"/> with <see cref="HttpRequest.Method"/> HTTP verb
    /// and <see cref="HttpRequest.Headers"/>.
    /// </summary>
    /// <param name="httpRequest">The HTTP request to be executed.</param>
    /// <param name="uploadProgress">Upload progress callback.</param>
    /// <param name="downloadProgress">Download progress callback.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that resolves to Htt</returns>
    Task<Tuple<HttpStatusCode, string>> ExecuteAsync(HttpRequest httpRequest,
        IProgress<ParseUploadProgressEventArgs> uploadProgress,
        IProgress<ParseDownloadProgressEventArgs> downloadProgress,
        CancellationToken cancellationToken);
  }
}
