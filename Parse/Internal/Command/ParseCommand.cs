// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Parse.Internal {
  /// <summary>
  /// ParseCommand is an <see cref="HttpRequest"/> with pre-populated
  /// headers.
  /// </summary>
  internal class ParseCommand : HttpRequest {
    private const string revocableSessionTokenTrueValue = "1";

    public IDictionary<string, object> DataObject { get; private set; }
    public override Stream Data {
      get {
        if (base.Data != null) {
          return base.Data;
        }

        return base.Data = DataObject != null
          ? new MemoryStream(Encoding.UTF8.GetBytes(Json.Encode(DataObject)))
          : null;
      }
      internal set { base.Data = value; }
    }

    public ParseCommand(string relativeUri,
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

    public ParseCommand(string relativeUri,
        string method,
        string sessionToken = null,
        IList<KeyValuePair<string, string>> headers = null,
        Stream stream = null,
        string contentType = null) {
      Uri = new Uri(new Uri(ParseClient.CurrentConfiguration.Server), relativeUri);
      Method = method;
      Data = stream;

      // TODO (richardross): Inject configuration instead of using shared static here.
      Headers = new List<KeyValuePair<string, string>> {
        new KeyValuePair<string, string>("X-Parse-Application-Id", ParseClient.CurrentConfiguration.ApplicationId),
        new KeyValuePair<string, string>("X-Parse-Client-Version", ParseClient.VersionString),
        new KeyValuePair<string, string>("X-Parse-Installation-Id", ParseClient.InstallationId.ToString())
      };

      if (headers != null) {
        foreach (var header in headers) {
          Headers.Add(header);
        }
      }

      if (!string.IsNullOrEmpty(ParseClient.PlatformHooks.AppBuildVersion)) {
        Headers.Add(new KeyValuePair<string, string>("X-Parse-App-Build-Version", ParseClient.PlatformHooks.AppBuildVersion));
      }
      if (!string.IsNullOrEmpty(ParseClient.PlatformHooks.AppDisplayVersion)) {
        Headers.Add(new KeyValuePair<string, string>("X-Parse-App-Display-Version", ParseClient.PlatformHooks.AppDisplayVersion));
      }
      if (!string.IsNullOrEmpty(ParseClient.PlatformHooks.OSVersion)) {
        Headers.Add(new KeyValuePair<string, string>("X-Parse-OS-Version", ParseClient.PlatformHooks.OSVersion));
      }
      // TODO (richardross): I hate the idea of having this super tightly coupled static variable in here.
      // Lets eventually get rid of it.
      if (!string.IsNullOrEmpty(ParseClient.MasterKey)) {
        Headers.Add(new KeyValuePair<string, string>("X-Parse-Master-Key", ParseClient.MasterKey));
      } else {
        Headers.Add(new KeyValuePair<string, string>("X-Parse-Windows-Key", ParseClient.CurrentConfiguration.WindowsKey));
      }
      if (!string.IsNullOrEmpty(sessionToken)) {
        Headers.Add(new KeyValuePair<string, string>("X-Parse-Session-Token", sessionToken));
      }
      if (!string.IsNullOrEmpty(contentType)) {
        Headers.Add(new KeyValuePair<string, string>("Content-Type", contentType));
      }
      if (ParseUser.IsRevocableSessionEnabled) {
        Headers.Add(new KeyValuePair<string, string>("X-Parse-Revocable-Session", revocableSessionTokenTrueValue));
      }
    }
  }
}
