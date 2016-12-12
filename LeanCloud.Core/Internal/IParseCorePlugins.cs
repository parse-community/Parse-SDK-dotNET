// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using LeanCloud.Storage.Internal;
using System;

namespace LeanCloud.Core.Internal {
  public interface IAVCorePlugins {
    void Reset();

    IHttpClient HttpClient { get; }
    IAVCommandRunner CommandRunner { get; }
    IStorageController StorageController { get; }

    IAVCloudCodeController CloudCodeController { get; }
    IAVConfigController ConfigController { get; }
    IAVFileController FileController { get; }
    IAVObjectController ObjectController { get; }
    IAVQueryController QueryController { get; }
    IAVSessionController SessionController { get; }
    IAVUserController UserController { get; }
    IObjectSubclassingController SubclassingController { get; }
    IAVCurrentUserController CurrentUserController { get; }
    IInstallationIdController InstallationIdController { get; }
  }
}