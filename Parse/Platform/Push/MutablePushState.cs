using System;
using System.Collections.Generic;
using Parse.Abstractions.Platform.Push;
using Parse.Infrastructure.Utilities;

namespace Parse.Platform.Push
{
    public class MutablePushState : IPushState
    {
        public ParseQuery<ParseInstallation> Query { get; set; }
        public IEnumerable<string> Channels { get; set; }
        public DateTime? Expiration { get; set; }
        public TimeSpan? ExpirationInterval { get; set; }
        public DateTime? PushTime { get; set; }
        public IDictionary<string, object> Data { get; set; }
        public string Alert { get; set; }

        public IPushState MutatedClone(Action<MutablePushState> func)
        {
            MutablePushState clone = MutableClone();
            func(clone);
            return clone;
        }

        protected virtual MutablePushState MutableClone()
        {
            return new MutablePushState
            {
                Query = Query,
                Channels = Channels == null ? null : new List<string>(Channels),
                Expiration = Expiration,
                ExpirationInterval = ExpirationInterval,
                PushTime = PushTime,
                Data = Data == null ? null : new Dictionary<string, object>(Data),
                Alert = Alert
            };
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is MutablePushState))
                return false;

            MutablePushState other = obj as MutablePushState;
            return Equals(Query, other.Query) &&
                   Channels.CollectionsEqual(other.Channels) &&
                   Equals(Expiration, other.Expiration) &&
                   Equals(ExpirationInterval, other.ExpirationInterval) &&
                   Equals(PushTime, other.PushTime) &&
                   Data.CollectionsEqual(other.Data) &&
                   Equals(Alert, other.Alert);
        }

        public override int GetHashCode()
        {
            // TODO (richardross): Implement this.
            return 0;
        }
    }
}
