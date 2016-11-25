// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LeanCloud.Common.Internal;
using System.Linq;

namespace LeanCloud.Core.Internal {
  /// <summary>
  /// AVCommand is an <see cref="HttpRequest"/> with pre-populated
  /// headers.
  /// </summary>
  public class AVCommand : HttpRequest {
    public IDictionary<string, object> DataObject { get; private set; }
    public override Stream Data {
      get {
        if (base.Data != null) {
          return base.Data;
        }

        return base.Data = (DataObject != null
          ? new MemoryStream(Encoding.UTF8.GetBytes(Json.Encode(DataObject)))
          : null);
      }
      set { base.Data = value; }
    }

    public AVCommand(string relativeUri,
        string method,
        string sessionToken = null,
        IList<KeyValuePair<string, string>> headers = null,
        IDictionary<string, object> data = null) : this(relativeUri: relativeUri,
            method: method,
            sessionToken: sessionToken,
            headers: headers,
            stream: null,
            contentType: data != null ? "application/json" : null) {
      DataObject = data;
    }

    public AVCommand(string relativeUri,
        string method,
        string sessionToken = null,
        IList<KeyValuePair<string, string>> headers = null,
        Stream stream = null,
        string contentType = null) {
      Uri = new Uri(new Uri(AVClient.CurrentConfiguration.Server), relativeUri);
      Method = method;
      Data = stream;
      Headers = new List<KeyValuePair<string, string>>(headers ?? Enumerable.Empty<KeyValuePair<string, string>>());

      if (!string.IsNullOrEmpty(sessionToken)) {
        Headers.Add(new KeyValuePair<string, string>("X-LC-Session", sessionToken));
      }
      if (!string.IsNullOrEmpty(contentType)) {
        Headers.Add(new KeyValuePair<string, string>("Content-Type", contentType));
      }
    }

    public AVCommand(AVCommand other) {
      this.Uri = other.Uri;
      this.Method = other.Method;
      this.DataObject = other.DataObject;
      this.Headers = new List<KeyValuePair<string, string>>(other.Headers);
      this.Data = other.Data;
    }
  }
}
