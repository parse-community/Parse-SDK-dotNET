// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System.Collections.Generic;

namespace Parse
{
    /// <summary>
    /// Represents a session of a user for a Parse application.
    /// </summary>
    [ParseClassName("_Session")]
    public class ParseSession : ParseObject
    {
        static HashSet<string> ImmutableKeys { get; } = new HashSet<string> { "sessionToken", "createdWith", "restricted", "user", "expiresAt", "installationId" };

        protected override bool CheckKeyMutable(string key) => !ImmutableKeys.Contains(key);

        /// <summary>
        /// Gets the session token for a user, if they are logged in.
        /// </summary>
        [ParseFieldName("sessionToken")]
        public string SessionToken => GetProperty<string>(default, "SessionToken");
    }
}
