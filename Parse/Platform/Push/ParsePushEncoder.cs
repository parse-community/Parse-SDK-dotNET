using System;
using System.Collections.Generic;
using Parse.Abstractions.Platform.Push;
using Parse.Infrastructure.Utilities;

namespace Parse.Platform.Push
{
    public class ParsePushEncoder
    {
        public static ParsePushEncoder Instance { get; } = new ParsePushEncoder { };

        private ParsePushEncoder() { }

        public IDictionary<string, object> Encode(IPushState state)
        {
            if (state.Alert is null && state.Data is null)
                throw new InvalidOperationException("A push must have either an Alert or Data");

            if (state.Channels is null && state.Query is null)
                throw new InvalidOperationException("A push must have either Channels or a Query");

            IDictionary<string, object> data = state.Data ?? new Dictionary<string, object>
            {
                ["alert"] = state.Alert
            };

#pragma warning disable CS1030 // #warning directive
#warning Verify that it is fine to instantiate a ParseQuery<ParseInstallation> here with a default(IServiceHub).

            ParseQuery<ParseInstallation> query = state.Query ?? new ParseQuery<ParseInstallation>(default, "_Installation") { };
#pragma warning restore CS1030 // #warning directive

            if (state.Channels != null)
                query = query.WhereContainedIn("channels", state.Channels);

            Dictionary<string, object> payload = new Dictionary<string, object>
            {
                [nameof(data)] = data,
                ["where"] = query.BuildParameters().GetOrDefault("where", new Dictionary<string, object> { }),
            };

            if (state.Expiration.HasValue)
                payload["expiration_time"] = state.Expiration.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
            else if (state.ExpirationInterval.HasValue)
                payload["expiration_interval"] = state.ExpirationInterval.Value.TotalSeconds;

            if (state.PushTime.HasValue)
                payload["push_time"] = state.PushTime.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

            return payload;
        }
    }
}
