// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using Parse.Common.Internal;
using System;

namespace Parse.Core.Internal {
  public interface IParseCorePlugins {
    void Reset();

    IHttpClient HttpClient { get; }
    IParseCommandRunner CommandRunner { get; }
    IStorageController StorageController { get; }

    IParseCloudCodeController CloudCodeController { get; }
    IParseConfigController ConfigController { get; }
    IParseFileController FileController { get; }
    IParseObjectController ObjectController { get; }
    IParseQueryController QueryController { get; }
    IParseSessionController SessionController { get; }
    IParseUserController UserController { get; }
    IObjectSubclassingController SubclassingController { get; }
    IParseCurrentUserController CurrentUserController { get; }
    IInstallationIdController InstallationIdController { get; }
  }
}