// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using Parse.Core.Internal;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse.Common.Internal;

namespace Parse {
  /// <summary>
  /// Represents a session of a user for a Parse application.
  /// </summary>
  [ParseClassName("_Session")]
  public class ParseSession : ParseObject {
    private static readonly HashSet<string> readOnlyKeys = new HashSet<string> {
      "sessionToken", "createdWith", "restricted", "user", "expiresAt", "installationId"
    };

    protected override bool IsKeyMutable(string key) {
      return !readOnlyKeys.Contains(key);
    }

    /// <summary>
    /// Gets the session token for a user, if they are logged in.
    /// </summary>
    [ParseFieldName("sessionToken")]
    public string SessionToken {
      get { return GetProperty<string>(null, "SessionToken"); }
    }

    /// <summary>
    /// Constructs a <see cref="ParseQuery{ParseSession}"/> for ParseSession.
    /// </summary>
    public static ParseQuery<ParseSession> Query {
      get {
        return new ParseQuery<ParseSession>();
      }
    }

    internal static IParseSessionController SessionController {
      get {
        return ParseCorePlugins.Instance.SessionController;
      }
    }

    /// <summary>
    /// Gets the current <see cref="ParseSession"/> object related to the current user.
    /// </summary>
    public static Task<ParseSession> GetCurrentSessionAsync() {
      return GetCurrentSessionAsync(CancellationToken.None);
    }

    /// <summary>
    /// Gets the current <see cref="ParseSession"/> object related to the current user.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    public static Task<ParseSession> GetCurrentSessionAsync(CancellationToken cancellationToken) {
      return ParseUser.GetCurrentUserAsync().OnSuccess(t1 => {
        ParseUser user = t1.Result;
        if (user == null) {
          return Task<ParseSession>.FromResult((ParseSession)null);
        }

        string sessionToken = user.SessionToken;
        if (sessionToken == null) {
          return Task<ParseSession>.FromResult((ParseSession)null);
        }

        return SessionController.GetSessionAsync(sessionToken, cancellationToken).OnSuccess(t => {
          ParseSession session = ParseObject.FromState<ParseSession>(t.Result, "_Session");
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
        ParseSession session = ParseObject.FromState<ParseSession>(t.Result, "_Session");
        return session.SessionToken;
      });
    }
  }
}
