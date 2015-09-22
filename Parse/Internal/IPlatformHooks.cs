// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Parse.Internal {
  interface IPlatformHooks {
    /// <summary>
    /// A thread-safe dictionary that persists key-value pair objects to disk.
    /// </summary>
    IDictionary<string, object> ApplicationSettings { get; }
    IHttpClient HttpClient { get; }

    void Initialize();

    /// <summary>
    /// Executes platform specific hook that mutate the installation based on
    /// the device platforms.
    /// </summary>
    /// <param name="installation">Installation to be mutated.</param>
    /// <returns></returns>
    Task ExecuteParseInstallationSaveHookAsync(ParseInstallation installation);

    string SDKName { get; }
    string AppName { get; }
    string AppBuildVersion { get; }
    string AppDisplayVersion { get; }
    string AppIdentifier { get; }
    string OSVersion { get; }
    string DeviceType { get; }
    string DeviceTimeZone { get; }
  }
}
