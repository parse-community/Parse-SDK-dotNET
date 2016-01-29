// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Parse.Core.Internal {
  public interface IParseCommandRunner {
    /// <summary>
    /// Executes <see cref="ParseCommand"/> and convert the result into Dictionary.
    /// </summary>
    /// <param name="command">The command to be run.</param>
    /// <param name="uploadProgress">Upload progress callback.</param>
    /// <param name="downloadProgress">Download progress callback.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns></returns>
    Task<Tuple<HttpStatusCode, IDictionary<string, object>>> RunCommandAsync(ParseCommand command,
        IProgress<ParseUploadProgressEventArgs> uploadProgress = null,
        IProgress<ParseDownloadProgressEventArgs> downloadProgress = null,
        CancellationToken cancellationToken = default(CancellationToken));
  }
}
