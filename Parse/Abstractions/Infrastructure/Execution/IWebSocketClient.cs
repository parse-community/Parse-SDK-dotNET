using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Parse.Infrastructure.Execution;

namespace Parse.Abstractions.Infrastructure.Execution;

/// <summary>
/// Represents an interface for a WebSocket client to handle WebSocket connections and communications.
/// </summary>
public interface IWebSocketClient
{
    /// <summary>
    /// An event that is triggered when a message is received via the WebSocket connection.
    /// </summary>
    /// <remarks>
    /// The event handler receives the message as a string parameter. This can be used to process incoming
    /// WebSocket messages, such as notifications, commands, or data updates.
    /// </remarks>
    public event EventHandler<MessageReceivedEventArgs> MessageReceived;

    /// <summary>
    /// An event that is triggered when an error occurs during the WebSocket operation.
    /// </summary>
    /// <remarks>
    /// This event communicates WebSocket-specific errors along with additional details encapsulated in
    /// the <see cref="ErrorEventArgs"/> object. It can be used to handle and log errors during WebSocket
    /// communication or connection lifecycle.
    /// </remarks>
    public event EventHandler<ErrorEventArgs> WebsocketError;

    /// <summary>
    /// An event that is triggered when an unknown or unexpected error occurs during WebSocket communication.
    /// </summary>
    /// <remarks>
    /// This event can be used to handle errors that do not fall under typical WebSocket error events. The event
    /// handler receives an <see cref="ErrorEventArgs"/> parameter containing details about the error.
    /// </remarks>
    public event EventHandler<ErrorEventArgs> UnknownError;

    /// <summary>
    /// Establishes a WebSocket connection to the specified server URI.
    /// </summary>
    /// <param name="serverUri">The URI of the WebSocket server to connect to.</param>
    /// <param name="cancellationToken">
    /// A token to observe cancellation requests. The operation will stop if the token is canceled.
    /// </param>
    /// <returns>A task that represents the asynchronous operation of opening the WebSocket connection.</returns>
    public Task OpenAsync(string serverUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the active WebSocket connection asynchronously.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to observe cancellation requests. The operation will stop if the token is canceled.
    /// </param>
    /// <returns>A task that represents the asynchronous operation of closing the WebSocket connection.</returns>
    public Task CloseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message over the established WebSocket connection asynchronously.
    /// </summary>
    /// <param name="message">The message to send through the WebSocket connection.</param>
    /// <param name="cancellationToken">
    /// A token to observe cancellation requests. The operation will stop if the token is canceled.
    /// </param>
    /// <returns>A task that represents the asynchronous operation of sending the message.</returns>
    /// <exception cref="InvalidOperationException">Thrown when trying to send a message on a WebSocket connection that is not in the Open state.</exception>
    public Task SendAsync(string message, CancellationToken cancellationToken = default);
}