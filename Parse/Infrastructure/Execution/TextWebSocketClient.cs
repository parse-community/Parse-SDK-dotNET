using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure.Execution;

namespace Parse.Infrastructure.Execution;

/// <summary>
/// Represents a WebSocket client that allows connecting to a WebSocket server, sending messages, and receiving messages.
/// Implements the <c>IWebSocketClient</c> interface for WebSocket operations.
/// </summary>
class TextWebSocketClient : IWebSocketClient
{
    /// <summary>
    /// A private instance of the ClientWebSocket class used to manage the WebSocket connection.
    /// This variable is responsible for handling the low-level WebSocket communication, including
    /// connecting, sending, and receiving data from the WebSocket server. It is initialized
    /// when establishing a connection and is used internally for operations such as sending messages
    /// and listening for incoming data.
    /// </summary>
    private ClientWebSocket _webSocket;

    /// <summary>
    /// A private instance of the Task class representing the background operation
    /// responsible for continuously listening for incoming WebSocket messages.
    /// This task is used to manage the asynchronous listening process, ensuring that
    /// messages are received from the WebSocket server without blocking the main thread.
    /// It is initialized when the listening process starts and monitored to prevent
    /// multiple concurrent listeners from being created.
    /// </summary>
    private Task _listeningTask;

    /// <summary>
    /// An event triggered whenever a message is received from the WebSocket server.
    /// This event is used to notify subscribers with the content of the received message,
    /// represented as a string. Handlers for this event can process or respond to the message
    /// based on the application's requirements.
    /// </summary>
    public event EventHandler<string> MessageReceived;

    /// <summary>
    /// Opens a WebSocket connection to the specified server URI and starts listening for messages.
    /// If the connection is already open or in a connecting state, this method does nothing.
    /// </summary>
    /// <param name="serverUri">The URI of the WebSocket server to connect to.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the connect operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation of connecting to the WebSocket server.
    /// </returns>
    public async Task OpenAsync(string serverUri, CancellationToken cancellationToken = default)
    {
        _webSocket ??= new ClientWebSocket();

        Debug.WriteLine($"Status: {_webSocket.State.ToString()}");
        if (_webSocket.State != WebSocketState.Open && _webSocket.State != WebSocketState.Connecting)
        {
            Debug.WriteLine($"Connecting to: {serverUri}");
            await _webSocket.ConnectAsync(new Uri(serverUri), cancellationToken);
            Debug.WriteLine($"Status: {_webSocket.State.ToString()}");
            StartListening(cancellationToken);
        }
    }

    /// <summary>
    /// Closes the WebSocket connection gracefully with a normal closure status.
    /// Ensures that the WebSocket connection is properly terminated and resources are released.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the close operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation of closing the WebSocket connection.
    /// </returns>
    public async Task CloseAsync(CancellationToken cancellationToken = default)
        => await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, String.Empty, cancellationToken);

    private async Task ListenForMessages(CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[1024 * 4];

        try
        {
            while (!cancellationToken.IsCancellationRequested &&
                   _webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await _webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await CloseAsync(cancellationToken);
                    break;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                MessageReceived?.Invoke(this, message);
            }
        }
        catch (OperationCanceledException ex)
        {
            // Normal cancellation, no need to handle
            Debug.WriteLine($"ClientWebsocket connection was closed: {ex.Message}");
        }
    }


    /// <summary>
    /// Starts listening for incoming messages from the WebSocket connection. This method ensures that only one listener task is running at a time.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to signal the listener task to stop.</param>
    private void StartListening(CancellationToken cancellationToken)
    {
        // Make sure we don't start multiple listeners
        if (_listeningTask is { IsCompleted: false })
        {
            return;
        }

        // Start the listener task
        _listeningTask = Task.Run(async () =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            await ListenForMessages(cancellationToken);
        }, cancellationToken);
    }

    /// <summary>
    /// Sends a text message to the connected WebSocket server asynchronously.
    /// The message is encoded in UTF-8 format before being sent.
    /// </summary>
    /// <param name="message">The message to be sent to the WebSocket server.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the send operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation of sending the message to the WebSocket server.
    /// </returns>
    public async Task SendAsync(string message, CancellationToken cancellationToken = default)
    {
        Debug.WriteLine($"Sending: {message}");
        if (_webSocket is not null && _webSocket.State == WebSocketState.Open)
        {
            await _webSocket.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, cancellationToken);
            Console.WriteLine("Sent");
        }
    }
}