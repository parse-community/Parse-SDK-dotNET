using System;
using System.Collections.Generic;
using Parse.Abstractions.Infrastructure;

namespace Parse.Infrastructure;

/// <summary>
/// Represents the configuration of the Parse Live Query server.
/// </summary>
public struct LiveQueryServerConnectionData : ILiveQueryServerConnectionData
{
    public LiveQueryServerConnectionData() { }

    internal bool Test { get; set; }

    /// <summary>
    /// The timeout duration, in milliseconds, used for various operations, such as
    /// establishing a connection or completing a subscription.
    /// </summary>
    public TimeSpan Timeout { get; set; } = ILiveQueryServerConnectionData.DefaultTimeout;

    /// <summary>
    /// The buffer size, in bytes, used by the WebSocket client for communication operations.
    /// </summary>
    public int MessageBufferSize { get; set; } = ILiveQueryServerConnectionData.DefaultBufferSize;

    /// <summary>
    /// The App ID of your app.
    /// </summary>
    public string ApplicationID { get; set; }

    /// <summary>
    /// A URI pointing to the target Parse Server instance hosting the app targeted by <see cref="ApplicationID"/>.
    /// </summary>
    public string ServerURI { get; set; }

    /// <summary>
    /// The .NET Key for the Parse app targeted by <see cref="ServerURI"/>.
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// The Master Key for the Parse app targeted by <see cref="Key"/>.
    /// </summary>
    public string MasterKey { get; set; }

    /// <summary>
    /// Additional HTTP headers to be sent with network requests from the SDK.
    /// </summary>
    public IDictionary<string, string> Headers { get; set; }
}
