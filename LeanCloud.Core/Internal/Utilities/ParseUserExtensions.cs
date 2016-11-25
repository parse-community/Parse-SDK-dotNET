// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LeanCloud.Core.Internal {
  /// <summary>
  /// So here's the deal. We have a lot of internal APIs for AVObject, AVUser, etc.
  ///
  /// These cannot be 'internal' anymore if we are fully modularizing things out, because
  /// they are no longer a part of the same library, especially as we create things like
  /// Installation inside push library.
  ///
  /// So this class contains a bunch of extension methods that can live inside another
  /// namespace, which 'wrap' the intenral APIs that already exist.
  /// </summary>
  public static class AVUserExtensions {
    public static IDictionary<string, IDictionary<string, object>> GetAuthData(this AVUser user) {
      return user.AuthData;
    }

    public static Task UnlinkFromAsync(this AVUser user, string authType, CancellationToken cancellationToken) {
      return user.UnlinkFromAsync(authType, cancellationToken);
    }

    public static Task<AVUser> LogInWithAsync(string authType, CancellationToken cancellationToken) {
      return AVUser.LogInWithAsync(authType, cancellationToken);
    }

    public static Task<AVUser> LogInWithAsync(string authType, IDictionary<string, object> data, CancellationToken cancellationToken) {
      return AVUser.LogInWithAsync(authType, data, cancellationToken);
    }

    public static Task LinkWithAsync(this AVUser user, string authType, CancellationToken cancellationToken) {
      return user.LinkWithAsync(authType, cancellationToken);
    }

    public static Task LinkWithAsync(this AVUser user, string authType, IDictionary<string, object> data, CancellationToken cancellationToken) {
      return user.LinkWithAsync(authType, data, cancellationToken);
    }

    public static Task UpgradeToRevocableSessionAsync(this AVUser user, CancellationToken cancellationToken) {
      return user.UpgradeToRevocableSessionAsync(cancellationToken);
    }
  }
}
