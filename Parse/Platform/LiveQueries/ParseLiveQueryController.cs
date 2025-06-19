using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Platform.LiveQueries;
using Parse.Infrastructure.Utilities;

namespace Parse.Platform.LiveQueries;

public class ParseLiveQueryController : IParseLiveQueryController
{
    IWebSocketClient WebSocketClient { get; }

    private int LastRequestId { get; set; } = 0;

    public int TimeOut { get; set; } = 5000;

    // public event EventHandler Connected;
    // public event EventHandler<int> Subscribed;
    // public event EventHandler<int> Unsubscribed;
    // public event EventHandler<int> SubscribtionUpdated;
    public event EventHandler<IDictionary<string, object>> Error;
    public event EventHandler<IDictionary<string, object>> Create;
    public event EventHandler<IDictionary<string, object>> Enter;
    public event EventHandler<IDictionary<string, object>> Update;
    public event EventHandler<IDictionary<string, object>> Leave;
    public event EventHandler<IDictionary<string, object>> Delete;

    public enum ParseLiveQueryState
    {
        /// <summary>
        /// Represents the state where the live query connection is closed.
        /// This indicates that any active connection to the live query server
        /// has been terminated, and no data updates are being received.
        /// </summary>
        Closed,
        Connecting,
        Connected
    }

    public ParseLiveQueryState State { get; private set; }
    public ArrayList SubscriptionIds { get; }

    CancellationTokenSource ConnectionSignal { get; set; }
    private IDictionary<int, CancellationTokenSource> SubscriptionSignals { get; } = new Dictionary<int, CancellationTokenSource> { };
    private IDictionary<int, CancellationTokenSource> UnsubscriptionSignals { get; } = new Dictionary<int, CancellationTokenSource> { };
    private IDictionary<int, CancellationTokenSource> SubscriptionUpdateSignals { get; } = new Dictionary<int, CancellationTokenSource> { };

    public ParseLiveQueryController(IWebSocketClient webSocketClient)
    {
        WebSocketClient = webSocketClient;
        SubscriptionIds = new ArrayList();
        State = ParseLiveQueryState.Closed;
    }

    /// <summary>
    /// Processes an incoming message by determining its operation type and triggering
    /// the corresponding events or handling the operation accordingly.
    /// </summary>
    /// <param name="message">
    /// A dictionary representing the message received, where the key-value pairs
    /// contain the details of the message including the operation type ("op") and
    /// any associated data.
    /// </param>
    private void ProcessMessage(IDictionary<string, object> message)
    {
        switch (message["op"])
        {
            case "connected":
                State = ParseLiveQueryState.Connected;
                ConnectionSignal?.Cancel();
                // Connected?.Invoke(this, EventArgs.Empty);
                break;

            case "subscribed":
                int requestId = Convert.ToInt32(message["requestId"]);
                SubscriptionIds.Add(requestId);
                if (SubscriptionSignals.TryGetValue(requestId, out CancellationTokenSource subscriptionSignal))
                {
                    subscriptionSignal?.Cancel();
                }
                // Subscribed?.Invoke(this, requestId);
                break;

            case "unsubscribed":
                requestId = Convert.ToInt32(message["requestId"]);
                SubscriptionIds.Remove(requestId);
                if (UnsubscriptionSignals.TryGetValue(requestId, out CancellationTokenSource unsubscriptionSignal))
                {
                    unsubscriptionSignal?.Cancel();
                }
                // Unsubscribed?.Invoke(this, requestId);
                break;

            case "error":
                if ((bool)message["reconnect"])
                {
                    OpenAsync();
                }
                string errorMessage = message["error"] as string;
                Error?.Invoke(this, message);
                break;

            case "create":
                Create?.Invoke(this, message);
                break;

            case "enter":
                Enter?.Invoke(this, message);
                break;

            case "update":
                Update?.Invoke(this, message);
                break;

            case "leave":
                Leave?.Invoke(this, message);
                break;

            case "delete":
                Delete?.Invoke(this, message);
                break;

            default:
                Debug.WriteLine($"Unknown operation: {message["op"]}");
                break;
        }
    }

    private IDictionary<string, object> AppendSessionToken(IDictionary<string, object> message)
    {
        return message.Concat(new Dictionary<string, object> {
            { "sessionToken", ParseClient.Instance.Services.GetCurrentSessionToken() }
        }).ToDictionary();
    }

    /// <summary>
    /// Sends a message to the server over a WebSocket connection.
    /// This method processes the message data and ensures it's transmitted asynchronously.
    /// </summary>
    /// <param name="message">
    /// A dictionary containing the message data to be sent.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the task to complete, allowing the operation to be canceled.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation of sending the message.
    /// </returns>
    private async Task SendMessage(IDictionary<string, object> message, CancellationToken cancellationToken)
    {
        await WebSocketClient.SendAsync(JsonUtilities.Encode(AppendSessionToken(message)), cancellationToken);
    }

    /// <summary>
    /// Opens a WebSocket connection and initiates listening for updates.
    /// This method establishes the connection to the Live Query server and transitions the state to Open.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to observe while waiting for the task to complete. It allows canceling the connection process.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation of opening the WebSocket connection.
    /// </returns>
    private async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        if (ParseClient.Instance.Services == null)
        {
            throw new InvalidOperationException("ParseClient.Services must be initialized before connecting to the LiveQuery server.");
        }

        if (ParseClient.Instance.Services.LiveQueryServerConnectionData == null)
        {
            throw new InvalidOperationException("ParseClient.Services.LiveQueryServerConnectionData must be initialized before connecting to the LiveQuery server.");
        }

        await WebSocketClient.OpenAsync(ParseClient.Instance.Services.LiveQueryServerConnectionData.ServerURI, cancellationToken);
    }

    /// <summary>
    /// Handles a message received event from the WebSocket client by parsing the incoming message
    /// and processing it as a dictionary of key-value pairs.
    /// </summary>
    /// <param name="sender">
    /// The source of the event, typically the WebSocket client that triggered the message received event.
    /// </param>
    /// <param name="e">
    /// The raw string message received from the WebSocket client, representing JSON data
    /// that can be parsed and processed.
    /// </param>
    void WebSocketClientOnMessageReceived(object sender, string e)
        => ProcessMessage(JsonUtilities.Parse(e) as IDictionary<string, object>);

    /// <summary>
    /// Establishes a live query connection using the specified user credentials.
    /// This method initializes the connection and sends the required configuration message.
    /// </summary>
    /// <param name="user">
    /// The authenticated Parse user initiating the live query connection.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests during the connection process.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation of connecting to the live query service.
    /// </returns>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (State == ParseLiveQueryState.Closed)
        {
            State = ParseLiveQueryState.Connecting;
            await OpenAsync(cancellationToken);
            WebSocketClient.MessageReceived += WebSocketClientOnMessageReceived;
            Dictionary<string, object> message = new Dictionary<string, object>
            {
                { "op", "connect" },
                { "applicationId", ParseClient.Instance.Services.LiveQueryServerConnectionData.ApplicationID },
                { "clientKey", ParseClient.Instance.Services.LiveQueryServerConnectionData.Key }
            };
            await SendMessage(message, cancellationToken);
            ConnectionSignal = new CancellationTokenSource();
            bool signalReceived = ConnectionSignal.Token.WaitHandle.WaitOne(TimeOut);
            State = ParseLiveQueryState.Connected;
            ConnectionSignal.Dispose();
            if (!signalReceived)
            {
                throw new TimeoutException();
            }
        }
    }

    public async Task<int> SubscribeAsync<T>(ParseLiveQuery<T> liveQuery, CancellationToken cancellationToken = default) where T : ParseObject
    {
        Dictionary<string, object> message = new Dictionary<string, object>
        {
            { "op", "subscribe" },
            { "requestId", ++LastRequestId },
            { "query", liveQuery.BuildParameters(true) }
        };
        await SendMessage(message, cancellationToken);
        CancellationTokenSource completionSignal = new CancellationTokenSource();
        SubscriptionSignals.Add(LastRequestId, completionSignal);
        bool signalReceived = completionSignal.Token.WaitHandle.WaitOne(TimeOut);
        SubscriptionSignals.Remove(LastRequestId);
        completionSignal.Dispose();
        if (signalReceived)
        {
            return LastRequestId;
        }
        throw new TimeoutException();
    }

    public async Task UpdateSubscriptionAsync<T>(ParseLiveQuery<T> liveQuery, int requestId, CancellationToken cancellationToken = default) where T : ParseObject
    {
        Dictionary<string, object> message = new Dictionary<string, object>
        {
            { "op", "update" },
            { "requestId", requestId },
            { "query", liveQuery.BuildParameters(true) }
        };
        await SendMessage(message, cancellationToken);
    }

    public async Task UnsubscribeAsync(int requestId, CancellationToken cancellationToken = default)
    {
        Dictionary<string, object> message = new Dictionary<string, object>
        {
            { "op", "unsubscribe" },
            { "requestId", requestId }
        };
        await SendMessage(message, cancellationToken);
        CancellationTokenSource completionSignal = new CancellationTokenSource();
        UnsubscriptionSignals.Add(requestId, completionSignal);
        bool signalReceived = completionSignal.Token.WaitHandle.WaitOne(TimeOut);
        UnsubscriptionSignals.Remove(requestId);
        completionSignal.Dispose();
        if (!signalReceived)
        {
            throw new TimeoutException();
        }
    }

    /// <summary>
    /// Closes the WebSocket connection asynchronously and updates the state to reflect that the connection has been closed.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token that can be used to propagate notification that the operation should be canceled.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation of closing the WebSocket connection.
    /// </returns>
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (SubscriptionIds.Count == 0)
        {
            await WebSocketClient.CloseAsync(cancellationToken);
            State = ParseLiveQueryState.Closed;
        }
    }
}