// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Android.OS;
using Parse.Internal;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

using PreserveAttribute = Android.Runtime.PreserveAttribute;

namespace Parse {
  [Preserve(AllMembers = true)]
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
        return "xamarin-android";
      }
    }

    public string AppName {
      get {
        return ManifestInfo.DisplayName;
      }
    }

    public string AppBuildVersion {
      get {
        return ManifestInfo.VersionCode.ToString();
      }
    }

    public string AppDisplayVersion {
      get {
        return ManifestInfo.VersionName;
      }
    }

    public string AppIdentifier {
      get {
        return ManifestInfo.PackageName;
      }
    }

    public string OSVersion {
      get {
        return Build.VERSION.Release;
      }
    }

    public string DeviceType {
      get {
        return "android";
      }
    }

    public string DeviceTimeZone {
      get {
        return Java.Util.TimeZone.Default.ID;
      }
    }

    public void Initialize() {
      if (ManifestInfo.HasPermissionForGCM()) {
        GcmRegistrar.GetInstance().Register();
      }
    }

    public Task ExecuteParseInstallationSaveHookAsync(ParseInstallation installation) {
      // Do nothing.
      return Task.FromResult(0);
    }
  }
}