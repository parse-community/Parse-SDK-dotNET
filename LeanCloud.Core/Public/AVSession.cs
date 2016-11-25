// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using LeanCloud.Core.Internal;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Common.Internal;

namespace LeanCloud {
  /// <summary>
  /// Represents a session of a user for a LeanCloud application.
  /// </summary>
  [AVClassName("_Session")]
  public class AVSession : AVObject {
    private static readonly HashSet<string> readOnlyKeys = new HashSet<string> {
      "sessionToken", "createdWith", "restricted", "user", "expiresAt", "installationId"
    };

    protected override bool IsKeyMutable(string key) {
      return !readOnlyKeys.Contains(key);
    }

    /// <summary>
    /// Gets the session token for a user, if they are logged in.
    /// </summary>
    [AVFieldName("sessionToken")]
    public string SessionToken {
      get { return GetProperty<string>(null, "SessionToken"); }
    }

    /// <summary>
    /// Constructs a <see cref="ParseQuery{ParseSession}"/> for AVSession.
    /// </summary>
    public static AVQuery<AVSession> Query {
      get {
        return new AVQuery<AVSession>();
      }
    }

    internal static IAVSessionController SessionController {
      get {
        return AVPlugins.Instance.SessionController;
      }
    }

    /// <summary>
    /// Gets the current <see cref="ParseSession"/> object related to the current user.
    /// </summary>
    public static Task<AVSession> GetCurrentSessionAsync() {
      return GetCurrentSessionAsync(CancellationToken.None);
    }

    /// <summary>
    /// Gets the current <see cref="ParseSession"/> object related to the current user.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    public static Task<AVSession> GetCurrentSessionAsync(CancellationToken cancellationToken) {
      return AVUser.GetCurrentUserAsync().OnSuccess(t1 => {
        AVUser user = t1.Result;
        if (user == null) {
          return Task<AVSession>.FromResult((AVSession)null);
        }

        string sessionToken = user.SessionToken;
        if (sessionToken == null) {
          return Task<AVSession>.FromResult((AVSession)null);
        }

        return SessionController.GetSessionAsync(sessionToken, cancellationToken).OnSuccess(t => {
          AVSession session = AVObject.FromState<AVSession>(t.Result, "_Session");
          return session;
        });
      }).Unwrap();
    }

    internal static Task RevokeAsync(string sessionToken, CancellationToken cancellationToken) {
      if (sessionToken == null || !SessionController.IsRevocableSessionToken(sessionToken)) {
        return Task.FromResult(0);
      }
      return SessionController.RevokeAsync(sessionToken, cancellationToken);
    }

    internal static Task<string> UpgradeToRevocableSessionAsync(string sessionToken, CancellationToken cancellationToken) {
      if (sessionToken == null || SessionController.IsRevocableSessionToken(sessionToken)) {
        return Task<string>.FromResult(sessionToken);
      }

      return SessionController.UpgradeToRevocableSessionAsync(sessionToken, cancellationToken).OnSuccess(t => {
        AVSession session = AVObject.FromState<AVSession>(t.Result, "_Session");
        return session.SessionToken;
      });
    }
  }
}
