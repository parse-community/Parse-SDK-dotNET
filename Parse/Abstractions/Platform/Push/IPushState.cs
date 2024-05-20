using System;
using System.Collections.Generic;
using Parse.Platform.Push;

namespace Parse.Abstractions.Platform.Push
{
    public interface IPushState
    {
        ParseQuery<ParseInstallation> Query { get; }
        IEnumerable<string> Channels { get; }
        DateTime? Expiration { get; }
        TimeSpan? ExpirationInterval { get; }
        DateTime? PushTime { get; }
        IDictionary<string, object> Data { get; }
        string Alert { get; }

        IPushState MutatedClone(Action<MutablePushState> func);
    }
}
