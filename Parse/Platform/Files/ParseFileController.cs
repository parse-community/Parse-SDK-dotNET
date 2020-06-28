// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Platform.Files;
using Parse.Infrastructure.Execution;
using Parse.Infrastructure.Utilities;

namespace Parse.Platform.Files
{
    public class ParseFileController : IParseFileController
    {
        IParseCommandRunner CommandRunner { get; }

        public ParseFileController(IParseCommandRunner commandRunner) => CommandRunner = commandRunner;

        public Task<FileState> SaveAsync(FileState state, Stream dataStream, string sessionToken, IProgress<IDataTransferLevel> progress, CancellationToken cancellationToken = default)
        {
            if (state.Location != null)
                // !isDirty

                return Task.FromResult(state);

            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<FileState>(cancellationToken);

            long oldPosition = dataStream.Position;

            return CommandRunner.RunCommandAsync(new ParseCommand($"files/{state.Name}", method: "POST", sessionToken: sessionToken, contentType: state.MediaType, stream: dataStream), uploadProgress: progress, cancellationToken: cancellationToken).OnSuccess(uploadTask =>
            {
                Tuple<HttpStatusCode, IDictionary<string, object>> result = uploadTask.Result;
                IDictionary<string, object> jsonData = result.Item2;
                cancellationToken.ThrowIfCancellationRequested();

                return new FileState
                {
                    Name = jsonData["name"] as string,
                    Location = new Uri(jsonData["url"] as string, UriKind.Absolute),
                    MediaType = state.MediaType
                };
            }).ContinueWith(task =>
            {
                // Rewind the stream on failure or cancellation (if possible).

                if ((task.IsFaulted || task.IsCanceled) && dataStream.CanSeek)
                    dataStream.Seek(oldPosition, SeekOrigin.Begin);

                return task;
            }).Unwrap();
        }
    }
}
