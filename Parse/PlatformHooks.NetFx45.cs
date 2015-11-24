// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using Parse.Internal;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Parse {
  partial class PlatformHooks : IPlatformHooks {
    private IHttpClient httpClient = null;
    public IHttpClient HttpClient {
      get {
        httpClient = httpClient ?? new HttpClient();
        return httpClient;
      }
    }

    public string SDKName {
      get {
        return "netfx";
      }
    }

    public string AppName {
      get {
        var assembly = Assembly.GetEntryAssembly();
        if (assembly == null) {
          return null;
        } else {
          return assembly.GetName().Name;
        }
      }
    }

    public string AppBuildVersion {
      get {
        var assembly = Assembly.GetEntryAssembly();
        if (assembly == null) {
          return null;
        } else {
          return assembly.GetName().Version.ToString();
        }
      }
    }

    public string AppDisplayVersion {
      get {
        var assembly = Assembly.GetEntryAssembly();
        if (assembly == null) {
          return null;
        } else {
          return assembly.GetName().Version.ToString();
        }
      }
    }

    public string AppIdentifier {
      get {
        ApplicationIdentity appId = AppDomain.CurrentDomain.ApplicationIdentity;
        if (appId == null) {
          return null;
        } else {
          return appId.FullName;
        }
      }
    }

    public string OSVersion {
      get {
        return Environment.OSVersion.ToString();
      }
    }

    public string DeviceType {
      get {
        return "dotnet";
      }
    }

    public string DeviceTimeZone {
      get {
        string windowsName = TimeZoneInfo.Local.StandardName;
        if (ParseInstallation.TimeZoneNameMap.ContainsKey(windowsName)) {
          return ParseInstallation.TimeZoneNameMap[windowsName];
        } else {
          return null;
        }
      }
    }

    public void Initialize() {
      // Do nothing.
    }

    public Task ExecuteParseInstallationSaveHookAsync(ParseInstallation installation) {
      // Do nothing.
      return Task.FromResult(0);
    }
  }
}
