// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;

namespace Parse.Core.Internal {
  public class FileState {
    private const string ParseFileSecureScheme = "https";
    private const string ParseFileSecureDomain = "files.parsetfss.com";

    public string Name { get; set; }
    public string MimeType { get; set; }
    public Uri Url { get; set; }
    public Uri SecureUrl {
      get {
        Uri uri = Url;
        if (uri != null && uri.Host == ParseFileSecureDomain) {
          return new UriBuilder(uri) {
            Scheme = ParseFileSecureScheme,
            Port = -1, // This makes URIBuilder assign the default port for the URL scheme.
          }.Uri;
        }
        return uri;
      }
    }
  }
}
