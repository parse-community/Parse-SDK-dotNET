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

        protected override bool CheckKeyMutable(string key)
        {
            return !ImmutableKeys.Contains(key);
        }

        /// <summary>
        /// Gets the session token for a user, if they are logged in.
        /// </summary>
        [ParseFieldName("sessionToken")]
        public string SessionToken => GetProperty<string>(default, "SessionToken");
    }
}
