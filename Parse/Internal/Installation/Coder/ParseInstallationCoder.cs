using System;
using System.Linq;
using System.Collections.Generic;
using Parse;
using Parse.Core.Internal;

namespace Parse.Push.Internal
{
    public class ParseInstallationCoder : IParseInstallationCoder
    {
        private static readonly ParseInstallationCoder instance = new ParseInstallationCoder();
        public static ParseInstallationCoder Instance
        {
            get
            {
                return instance;
            }
        }
        private const string ISO8601Format = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'";

        public IDictionary<string, object> Encode(ParseInstallation installation)
        {
            var state = installation.GetState();
            var data = PointerOrLocalIdEncoder.Instance.Encode(state.ToDictionary(x => x.Key, x => x.Value)) as IDictionary<string, object>;
            data["objectId"] = state.ObjectId;
            if (state.CreatedAt != null)
            {
                data["createdAt"] = state.CreatedAt.Value.ToString(ISO8601Format);
            }
            if (state.UpdatedAt != null)
            {
                data["updatedAt"] = state.UpdatedAt.Value.ToString(ISO8601Format);
            }
            return data;
        }

        public ParseInstallation Decode(IDictionary<string, object> data)
        {
            var state = ParseObjectCoder.Instance.Decode(data, ParseDecoder.Instance);
            return ParseObjectExtensions.FromState<ParseInstallation>(state, "_Installation");
        }
    }
}