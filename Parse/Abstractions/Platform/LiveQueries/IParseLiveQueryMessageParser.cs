using System.Collections.Generic;
using Parse.Abstractions.Platform.Objects;

namespace Parse.Abstractions.Platform.LiveQueries;

/// <summary>
/// Defines methods for parsing live query messages in the Parse platform.
/// </summary>
public interface IParseLiveQueryMessageParser
{
    /// <summary>
    /// Gets the client identifier from the specified message.
    /// </summary>
    /// <param name="message">The message containing the client identifier.</param>
    /// <returns>The client identifier as a string.</returns>
    public string GetClientId(IDictionary<string, object> message);

    /// <summary>
    /// Gets the request identifier from the specified message.
    /// </summary>
    /// <param name="message">The message containing the request identifier.</param>
    /// <returns>The request identifier as an integer.</returns>
    public int GetRequestId(IDictionary<string, object> message);

    /// <summary>
    /// Gets the object state from the specified message.
    /// </summary>
    /// <param name="message">The message containing the object state data.</param>
    /// <returns>The object state as an <see cref="IObjectState"/>.</returns>
    public IObjectState GetObjectState(IDictionary<string, object> message);

    /// <summary>
    /// Gets the original object state from the specified message.
    /// </summary>
    /// <param name="message">The message containing the original object state data.</param>
    /// <returns>The original object state as an <see cref="IObjectState"/>.</returns>
    public IObjectState GetOriginalState(IDictionary<string, object> message);

    /// <summary>
    /// Gets the error information from the specified message.
    /// </summary>
    /// <param name="message">The message containing error details.</param>
    /// <returns>
    /// A tuple containing the error code, error message, and a boolean indicating whether to reconnect.
    /// </returns>
    public (int code, string error, bool reconnect) GetError(IDictionary<string, object> message);
}