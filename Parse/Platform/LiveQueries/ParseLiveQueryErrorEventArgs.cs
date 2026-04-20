using System;

namespace Parse.Platform.LiveQueries;

/// <summary>
/// Represents the arguments for an error event that occurs during a live query in the Parse platform.
/// </summary>
public class ParseLiveQueryErrorEventArgs : EventArgs
{
    /// <summary>
    /// Gets the error message associated with a live query operation.
    /// </summary>
    /// <remarks>
    /// The <see cref="Error"/> property contains a description of the error that occurred during
    /// a live query operation. It can provide detailed information about the nature of the issue,
    /// which can be helpful for debugging or logging purposes.
    /// </remarks>
    public string Error { get; }

    /// <summary>
    /// Gets the error code associated with a live query operation.
    /// </summary>
    /// <remarks>
    /// The <see cref="Code"/> property contains a numerical identifier that represents
    /// the type or category of the error that occurred during a live query operation.
    /// This is used alongside the error message to provide detailed diagnostics or logging.
    /// </remarks>
    public int Code { get; }

    /// <summary>
    /// Gets a value indicating whether the client should attempt to reconnect
    /// after an error occurs during a live query operation.
    /// </summary>
    /// <remarks>
    /// The <see cref="Reconnect"/> property specifies whether a reconnection to the
    /// live query server is recommended or required following certain error events.
    /// This can be used to determine the client's behavior in maintaining a continuous
    /// connection with the server.
    /// </remarks>
    public bool Reconnect { get; }

    /// <summary>
    /// Gets the local exception that occurred during a live query operation, if any.
    /// </summary>
    /// <remarks>
    /// The <see cref="LocalException"/> property contains the exception instance that was thrown locally,
    /// providing additional context or details about the error that occurred during the live query operation.
    /// This property may be <c>null</c> if no local exception was thrown.
    /// </remarks>
    public Exception LocalException { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ParseLiveQueryErrorEventArgs"/> class.
    /// </summary>
    /// <param name="code">The error code associated with the live query operation.</param>
    /// <param name="error">The error message associated with the live query operation.</param>
    /// <param name="reconnect">A value indicating whether the client should attempt to reconnect.</param>
    /// <param name="localException">The local exception that occurred, if any.</param>
    internal ParseLiveQueryErrorEventArgs(int code, string error, bool reconnect, Exception localException = null)    {
        Error = error;
        Code = code;
        Reconnect = reconnect;
        LocalException = localException;
    }
}