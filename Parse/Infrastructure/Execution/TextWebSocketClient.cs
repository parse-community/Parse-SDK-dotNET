using System;
using System.Diagnostics;
using System.IO;
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
class TextWebSocketClient(int bufferSize) : IWebSocketClient, IDisposable
{
    /// <summary>
    /// A private instance of the ClientWebSocket class used to manage the WebSocket connection.
    /// This variable is responsible for handling the low-level WebSocket communication, including
    /// connecting, sending, and receiving data from the WebSocket server. It is initialized
    /// when establishing a connection and is used internally for operations such as sending messages
    /// and listening for incoming data.
    /// </summary>
    private ClientWebSocket webSocket;

    /// <summary>
    /// A private instance of the Task class representing the background operation
    /// responsible for continuously listening for incoming WebSocket messages.
    /// This task is used to manage the asynchronous listening process, ensuring that
    /// messages are received from the WebSocket server without blocking the main thread.
    /// It is initialized when the listening process starts and monitored to prevent
    /// multiple concurrent listeners from being created.
    /// </summary>
    private Task listeningTask;

    /// <summary>
    /// An event triggered whenever a message is received from the WebSocket server.
    /// This event is used to notify subscribers with the content of the received message,
    /// represented as a string. Handlers for this event can process or respond to the message
    /// based on the application's requirements.
    /// </summary>
    public event EventHandler<MessageReceivedEventArgs> MessageReceived;
    public event EventHandler<ErrorEventArgs> WebsocketError;
    public event EventHandler<ErrorEventArgs> UnknownError;

    private readonly object connectionLock = new object();

    private int BufferSize { get; } = bufferSize;

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
        ClientWebSocket webSocketToConnect = null;
        lock (connectionLock)
        {
            webSocket ??= new ClientWebSocket();
            if (webSocket.State != WebSocketState.Open && webSocket.State != WebSocketState.Connecting)
            {
                webSocketToConnect = webSocket;
            }
        }
        listeningCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (webSocketToConnect is not null)
        {
            await webSocketToConnect.ConnectAsync(new Uri(serverUri), cancellationToken);
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
    {
        await webSocket?.CloseAsync(WebSocketCloseStatus.NormalClosure, String.Empty, cancellationToken)!;
    }

    private async Task ListenForMessages(CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[BufferSize];

        try
        {
            while (!cancellationToken.IsCancellationRequested &&
                   webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (result.EndOfMessage)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    MessageReceived?.Invoke(this, new MessageReceivedEventArgs(message));
                }
                else
                {
                    // Accumulate bytes to handle UTF-8 characters split across boundaries
                    using var messageStream = new MemoryStream();
                    messageStream.Write(buffer, 0, result.Count);
                    while (!result.EndOfMessage)
                    {
                        result = await webSocket.ReceiveAsync(
                            new ArraySegment<byte>(buffer),
                            cancellationToken);
                        messageStream.Write(buffer, 0, result.Count);
                    }
                    string fullMessage = Encoding.UTF8.GetString(messageStream.ToArray());
                    MessageReceived?.Invoke(this, new MessageReceivedEventArgs(fullMessage));
                }
            }
        }
        catch (OperationCanceledException ex)
        {
            // Normal cancellation, no need to handle
            Debug.WriteLine($"Websocket connection was closed: {ex.Message}");
        }
        catch (WebSocketException ex)
        {
            // WebSocket error, notify the user
            Debug.WriteLine($"Websocket error ({ex.ErrorCode}): {ex.Message}");
            WebsocketError?.Invoke(this, new ErrorEventArgs(ex));
        }
        catch (Exception ex)
        {
            // Unexpected error, notify the user
            Debug.WriteLine($"Unexpected error in Websocket listener: {ex.Message}");
            UnknownError?.Invoke(this, new ErrorEventArgs(ex));
        }
        Debug.WriteLine("Websocket ListenForMessage stopped");
    }

    /// <summary>
    /// Starts listening for incoming messages from the WebSocket connection. This method ensures that only one listener task is running at a time.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to signal the listener task to stop.</param>
    private void StartListening(CancellationToken cancellationToken)
    {
        // Make sure we don't start multiple listeners
        if (listeningTask is { IsCompleted: false })
        {
            return;
        }

        // Start the listener task
        listeningTask = Task.Run(async () =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            await ListenForMessages(cancellationToken);
            Debug.WriteLine("Websocket listeningTask stopped");
        }, cancellationToken);

        _ = listeningTask.ContinueWith(task =>
        {
            if (!task.IsFaulted)
                return;
            Debug.WriteLine($"Websocket listener task faulted: {task.Exception}");
        }, TaskContinuationOptions.OnlyOnFaulted);
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
    /// <exception cref="ArgumentNullException">Thrown when the WebSocket instance is null.</exception>
    /// <exception cref="WebSocketException">Thrown when there is an error during the WebSocket communication.</exception>
    /// <exception cref="InvalidOperationException">Thrown when trying to send a message on a WebSocket connection that is not in the Open state.</exception>
    public async Task SendAsync(string message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(webSocket);
        if (webSocket.State != WebSocketState.Open)
        {
            throw new InvalidOperationException($"WebSocket is not in Open state. Current state: {webSocket.State}");
        }
        await webSocket.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, cancellationToken);
    }

    private CancellationTokenSource listeningCts;
    private bool disposed;
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                listeningCts?.Cancel();
                try
                {
                    listeningTask?.Wait(TimeSpan.FromSeconds(5));
                }
                catch { }
                
                webSocket?.Dispose();
                listeningCts?.Dispose();
            }
            disposed = true;
        }
    }
}