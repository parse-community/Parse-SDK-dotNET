// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Threading;

namespace Parse.Internal {
  class InstallationIdController : IInstallationIdController {
    private readonly object mutex = new object();
    private Guid? installationId;

    public void Set(Guid? installationId) {
      lock (mutex) {
        if (installationId == null) {
          ParseClient.PlatformHooks.ApplicationSettings.Remove("InstallationId");
        } else {
          ParseClient.PlatformHooks.ApplicationSettings["InstallationId"] = installationId.ToString();
        }
        this.installationId = installationId;
      }
    }

    public Guid? Get() {
      lock (mutex) {
        if (installationId != null) {
          return installationId;
        }
        object id;
        ParseClient.PlatformHooks.ApplicationSettings.TryGetValue("InstallationId", out id);
        try {
          installationId = new Guid((string)id);
        } catch (Exception) {
          var newInstallationId = Guid.NewGuid();
          Set(newInstallationId);
        }
        return installationId;
      }
    }

    public void Clear() {
      Set(null);
    }
  }
}
