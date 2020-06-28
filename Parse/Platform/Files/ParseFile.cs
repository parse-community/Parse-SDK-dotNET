// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Infrastructure.Utilities;
using Parse.Platform.Files;

namespace Parse
{
    public static class FileServiceExtensions
    {
        /// <summary>
        /// Saves the file to the Parse cloud.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static Task SaveFileAsync(this IServiceHub serviceHub, ParseFile file, CancellationToken cancellationToken = default) => serviceHub.SaveFileAsync(file, default, cancellationToken);

        /// <summary>
        /// Saves the file to the Parse cloud.
        /// </summary>
        /// <param name="progress">The progress callback.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static Task SaveFileAsync(this IServiceHub serviceHub, ParseFile file, IProgress<IDataTransferLevel> progress, CancellationToken cancellationToken = default) => file.TaskQueue.Enqueue(toAwait => serviceHub.FileController.SaveAsync(file.State, file.DataStream, serviceHub.GetCurrentSessionToken(), progress, cancellationToken), cancellationToken).OnSuccess(task => file.State = task.Result);

#warning Make serviceHub null by default once dependents properly inject it when needed.

        /// <summary>
        /// Saves the file to the Parse cloud.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static Task SaveAsync(this ParseFile file, IServiceHub serviceHub, CancellationToken cancellationToken = default) => serviceHub.SaveFileAsync(file, cancellationToken);

        /// <summary>
        /// Saves the file to the Parse cloud.
        /// </summary>
        /// <param name="progress">The progress callback.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static Task SaveAsync(this ParseFile file, IServiceHub serviceHub, IProgress<IDataTransferLevel> progress, CancellationToken cancellationToken = default) => serviceHub.SaveFileAsync(file, progress, cancellationToken);
    }

    /// <summary>
    /// ParseFile is a local representation of a file that is saved to the Parse cloud.
    /// </summary>
    /// <example>
    /// The workflow is to construct a <see cref="ParseFile"/> with data and a filename,
    /// then save it and set it as a field on a ParseObject:
    ///
    /// <code>
    /// var file = new ParseFile("hello.txt",
    ///     new MemoryStream(Encoding.UTF8.GetBytes("hello")));
    /// await file.SaveAsync();
    /// var obj = new ParseObject("TestObject");
    /// obj["file"] = file;
    /// await obj.SaveAsync();
    /// </code>
    /// </example>
    public class ParseFile : IJsonConvertible
    {
        internal FileState State { get; set; }

        internal Stream DataStream { get; }

        internal TaskQueue TaskQueue { get; } = new TaskQueue { };

        #region Constructor

#warning Make IServiceHub optionally null once all dependents are injecting it if necessary.

        internal ParseFile(string name, Uri uri, string mimeType = null) => State = new FileState
        {
            Name = name,
            Location = uri,
            MediaType = mimeType
        };

        /// <summary>
        /// Creates a new file from a byte array and a name.
        /// </summary>
        /// <param name="name">The file's name, ideally with an extension. The file name
        /// must begin with an alphanumeric character, and consist of alphanumeric
        /// characters, periods, spaces, underscores, or dashes.</param>
        /// <param name="data">The file's data.</param>
        /// <param name="mimeType">To specify the content-type used when uploading the
        /// file, provide this parameter.</param>
        public ParseFile(string name, byte[] data, string mimeType = null) : this(name, new MemoryStream(data), mimeType) { }

        /// <summary>
        /// Creates a new file from a stream and a name.
        /// </summary>
        /// <param name="name">The file's name, ideally with an extension. The file name
        /// must begin with an alphanumeric character, and consist of alphanumeric
        /// characters, periods, spaces, underscores, or dashes.</param>
        /// <param name="data">The file's data.</param>
        /// <param name="mimeType">To specify the content-type used when uploading the
        /// file, provide this parameter.</param>
        public ParseFile(string name, Stream data, string mimeType = null)
        {
            State = new FileState
            {
                Name = name,
                MediaType = mimeType
            };

            DataStream = data;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the file still needs to be saved.
        /// </summary>
        public bool IsDirty => State.Location == null;

        /// <summary>
        /// Gets the name of the file. Before save is called, this is the filename given by
        /// the user. After save is called, that name gets prefixed with a unique identifier.
        /// </summary>
        [ParseFieldName("name")]
        public string Name => State.Name;

        /// <summary>
        /// Gets the MIME type of the file. This is either passed in to the constructor or
        /// inferred from the file extension. "unknown/unknown" will be used if neither is
        /// available.
        /// </summary>
        public string MimeType => State.MediaType;

        /// <summary>
        /// Gets the url of the file. It is only available after you save the file or after
        /// you get the file from a <see cref="ParseObject"/>.
        /// </summary>
        [ParseFieldName("url")]
        public Uri Url => State.SecureLocation;

        #endregion

        IDictionary<string, object> IJsonConvertible.ConvertToJSON()
        {
            if (IsDirty)
            {
                throw new InvalidOperationException("ParseFile must be saved before it can be serialized.");
            }

            return new Dictionary<string, object>
            {
                ["__type"] = "File",
                ["name"] = Name,
                ["url"] = Url.AbsoluteUri
            };
        }
    }
}
