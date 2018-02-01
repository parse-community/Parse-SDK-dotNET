// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Linq;
using System.Collections.Generic;
using Parse.Common.Internal;
using Parse.Core.Internal;

namespace Parse.Push.Internal
{
    public class ParsePushEncoder
    {
        private static readonly ParsePushEncoder instance = new ParsePushEncoder();
        public static ParsePushEncoder Instance
        {
            get
            {
                return instance;
            }
        }

        private ParsePushEncoder() { }

        public IDictionary<string, object> Encode(IPushState state)
        {
            if (state.Alert == null && state.Data == null)
            {
                throw new InvalidOperationException("A push must have either an Alert or Data");
            }
            if (state.Channels == null && state.Query == null)
            {
                throw new InvalidOperationException("A push must have either Channels or a Query");
            }

            var data = state.Data ?? new Dictionary<string, object> { { "alert", state.Alert } };
            var query = state.Query ?? ParseInstallation.Query;
            if (state.Channels != null)
            {
                query = query.WhereContainedIn("channels", state.Channels);
            }
            var payload = new Dictionary<string, object> {
        { "data", data },
        { "where", query.BuildParameters().GetOrDefault("where", new Dictionary<string, object>()) },
      };
            if (state.Expiration.HasValue)
            {
                payload["expiration_time"] = state.Expiration.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
            }
            else if (state.ExpirationInterval.HasValue)
            {
                payload["expiration_interval"] = state.ExpirationInterval.Value.TotalSeconds;
            }
            if (state.PushTime.HasValue)
            {
                payload["push_time"] = state.PushTime.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            }

            return payload;
        }
    }
}
