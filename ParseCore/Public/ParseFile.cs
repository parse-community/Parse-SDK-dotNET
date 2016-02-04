// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using Parse.Core.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Parse.Common.Internal;

namespace Parse {
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
  public class ParseFile : IJsonConvertible {
    private FileState state;
    private readonly Stream dataStream;
    private readonly TaskQueue taskQueue = new TaskQueue();

    #region Constructor

    internal ParseFile(string name, Uri uri, string mimeType = null) {
      state = new FileState {
        Name = name,
        Url = uri,
        MimeType = mimeType
      };
    }

    /// <summary>
    /// Creates a new file from a byte array and a name.
    /// </summary>
    /// <param name="name">The file's name, ideally with an extension. The file name
    /// must begin with an alphanumeric character, and consist of alphanumeric
    /// characters, periods, spaces, underscores, or dashes.</param>
    /// <param name="data">The file's data.</param>
    /// <param name="mimeType">To specify the content-type used when uploading the
    /// file, provide this parameter.</param>
    public ParseFile(string name, byte[] data, string mimeType = null)
      : this(name, new MemoryStream(data), mimeType) { }

    /// <summary>
    /// Creates a new file from a stream and a name.
    /// </summary>
    /// <param name="name">The file's name, ideally with an extension. The file name
    /// must begin with an alphanumeric character, and consist of alphanumeric
    /// characters, periods, spaces, underscores, or dashes.</param>
    /// <param name="data">The file's data.</param>
    /// <param name="mimeType">To specify the content-type used when uploading the
    /// file, provide this parameter.</param>
    public ParseFile(string name, Stream data, string mimeType = null) {
      state = new FileState {
        Name = name,
        MimeType = mimeType
      };
      this.dataStream = data;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets whether the file still needs to be saved.
    /// </summary>
    public bool IsDirty {
      get {
        return state.Url == null;
      }
    }

    /// <summary>
    /// Gets the name of the file. Before save is called, this is the filename given by
    /// the user. After save is called, that name gets prefixed with a unique identifier.
    /// </summary>
    [ParseFieldName("name")]
    public string Name {
      get {
        return state.Name;
      }
    }

    /// <summary>
    /// Gets the MIME type of the file. This is either passed in to the constructor or
    /// inferred from the file extension. "unknown/unknown" will be used if neither is
    /// available.
    /// </summary>
    public string MimeType {
      get {
        return state.MimeType;
      }
    }

    /// <summary>
    /// Gets the url of the file. It is only available after you save the file or after
    /// you get the file from a <see cref="ParseObject"/>.
    /// </summary>
    [ParseFieldName("url")]
    public Uri Url {
      get {
        return state.SecureUrl;
      }
    }

    internal static IParseFileController FileController {
      get {
        return ParseCorePlugins.Instance.FileController;
      }
    }

    #endregion

    IDictionary<string, object> IJsonConvertible.ToJSON() {
      if (this.IsDirty) {
        throw new InvalidOperationException(
          "ParseFile must be saved before it can be serialized.");
      }
      return new Dictionary<string, object> {
        {"__type", "File"},
        {"name", Name},
        {"url", Url.AbsoluteUri}
      };
    }

    #region Save

    /// <summary>
    /// Saves the file to the Parse cloud.
    /// </summary>
    public Task SaveAsync() {
      return SaveAsync(null, CancellationToken.None);
    }

    /// <summary>
    /// Saves the file to the Parse cloud.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task SaveAsync(CancellationToken cancellationToken) {
      return SaveAsync(null, cancellationToken);
    }

    /// <summary>
    /// Saves the file to the Parse cloud.
    /// </summary>
    /// <param name="progress">The progress callback.</param>
    public Task SaveAsync(IProgress<ParseUploadProgressEventArgs> progress) {
      return SaveAsync(progress, CancellationToken.None);
    }

    /// <summary>
    /// Saves the file to the Parse cloud.
    /// </summary>
    /// <param name="progress">The progress callback.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task SaveAsync(IProgress<ParseUploadProgressEventArgs> progress,
        CancellationToken cancellationToken) {
      return taskQueue.Enqueue(
          toAwait => FileController.SaveAsync(state, dataStream, ParseUser.CurrentSessionToken, progress, cancellationToken), cancellationToken)
      .OnSuccess(t => {
        state = t.Result;
      });
    }

    #endregion
  }
}
