using System;
using System.Threading;
using System.Threading.Tasks;

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
    public event EventHandler<string> MessageReceived;

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
    public Task SendAsync(string message, CancellationToken cancellationToken);
}