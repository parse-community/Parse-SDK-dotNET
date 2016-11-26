// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Storage.Internal;

namespace LeanCloud.Core.Internal
{
    public class AVFileController : IAVFileController
    {
        private readonly IAVCommandRunner commandRunner;

        public AVFileController(IAVCommandRunner commandRunner)
        {
            this.commandRunner = commandRunner;
        }
        public Task<FileState> SaveAsync(FileState state,
        Stream dataStream,
        String sessionToken,
        IProgress<AVUploadProgressEventArgs> progress,
        CancellationToken cancellationToken = default(CancellationToken))
        {
            if (state.Url != null)
            {
                // !isDirty
                return Task<FileState>.FromResult(state);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                var tcs = new TaskCompletionSource<FileState>();
                tcs.TrySetCanceled();
                return tcs.Task;
            }

            var oldPosition = dataStream.Position;
            var command = new AVCommand("files/" + state.Name,
                method: "POST",
                sessionToken: sessionToken,
                contentType: state.MimeType,
                stream: dataStream);

            return commandRunner.RunCommandAsync(command,
                uploadProgress: progress,
                cancellationToken: cancellationToken).OnSuccess(uploadTask =>
                {
                    var result = uploadTask.Result;
                    var jsonData = result.Item2;
                    cancellationToken.ThrowIfCancellationRequested();

                    return new FileState
                    {
                        Name = jsonData["name"] as string,
                        Url = new Uri(jsonData["url"] as string, UriKind.Absolute),
                        MimeType = state.MimeType
                    };
                }).ContinueWith(t =>
                {
                    // Rewind the stream on failure or cancellation (if possible)
                    if ((t.IsFaulted || t.IsCanceled) && dataStream.CanSeek)
                    {
                        dataStream.Seek(oldPosition, SeekOrigin.Begin);
                    }
                    return t;
                }).Unwrap();
        }
        public Task DeleteAsync(FileState state, string sessionToken, CancellationToken cancellationToken)
        {
            var command = new AVCommand("files/" + state.ObjectId,
               method: "DELETE",
               sessionToken: sessionToken,
               data: null);

            return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken);
        }

        public Task<FileState> GetAsync(string objectId, string sessionToken, CancellationToken cancellationToken)
        {
            var command = new AVCommand("files/" + objectId,
                method: "GET",
                sessionToken: sessionToken,
                data: null);

            return commandRunner.RunCommandAsync(command, cancellationToken: cancellationToken).OnSuccess(_ =>
            {
                var result = _.Result;
                var jsonData = result.Item2;
                cancellationToken.ThrowIfCancellationRequested();
                return new FileState
                {
                    ObjectId = jsonData["objectId"] as string,
                    Name = jsonData["name"] as string,
                    Url = new Uri(jsonData["url"] as string, UriKind.Absolute),
                };
            });
        }
    }
}
