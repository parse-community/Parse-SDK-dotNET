namespace Parse.Abstractions.Infrastructure;

public interface ILiveQueryServerConnectionData : IServerConnectionData
{
    /// <summary>
    /// Represents the default timeout duration, in milliseconds.
    /// </summary>
    public const int DefaultTimeOut = 5000; // 5 seconds

    /// <summary>
    /// The timeout duration, in milliseconds, used for various operations, such as
    /// establishing a connection or completing a subscription.
    /// </summary>
    int TimeOut { get; set; }

    /// <summary>
    /// The default buffer size, in bytes.
    /// </summary>
    public const int DefaultBufferSize = 4096; // 4MB

    /// <summary>
    /// The buffer size, in bytes, used for the WebSocket operations to handle incoming messages.
    /// </summary>
    int MessageBufferSize { get; set; }
}
