using System;

namespace Parse.Abstractions.Infrastructure;

public interface ILiveQueryServerConnectionData : IServerConnectionData
{
    /// <summary>
    /// The timeout duration, in seconds, used for various operations, such as
    /// establishing a connection or completing a subscription.
    /// </summary>
    TimeSpan Timeout { get; set; }

    /// <summary>
    /// The default buffer size, in bytes.
    /// </summary>
    const int DefaultBufferSize = 4096; // 4KB

    /// <summary>
    /// The buffer size, in bytes, used for the WebSocket operations to handle incoming messages.
    /// </summary>
    int MessageBufferSize { get; set; }
}
