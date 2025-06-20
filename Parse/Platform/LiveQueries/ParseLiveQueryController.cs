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

/// <summary>
/// The ParseLiveQueryController is responsible for managing live query subscriptions, maintaining a connection
/// to the Parse LiveQuery server, and handling real-time updates from the server.
/// </summary>
public class ParseLiveQueryController : IParseLiveQueryController
{
    private IWebSocketClient WebSocketClient { get; }

    private int LastRequestId { get; set; } = 0;

    /// <summary>
    /// Gets or sets the timeout duration, in milliseconds, used by the ParseLiveQueryController
    /// for various operations, such as establishing a connection or completing a subscription.
    /// </summary>
    /// <remarks>
    /// This property determines the maximum amount of time the controller will wait for an operation
    /// to complete before throwing a <see cref="TimeoutException"/>. It is used in operations such as:
    /// - Connecting to the LiveQuery server.
    /// - Subscribing to a query.
    /// - Unsubscribing from a query.
    /// Ensure that the value is configured appropriately to avoid premature timeout errors in network-dependent processes.
    /// </remarks>
    public int TimeOut { get; set; } = 5000;

    /// <summary>
    /// Event triggered when an error occurs during Parse Live Query operations.
    /// </summary>
    /// <remarks>
    /// This event provides detailed information about the encountered error through the event arguments,
    /// which consist of a dictionary containing key-value pairs describing the error context and specifics.
    /// It can be used to log, handle, or analyze the errors that arise during subscription, connection,
    /// or message processing operations. Common scenarios triggering this event include protocol issues,
    /// connectivity problems, or invalid message formats.
    /// </remarks>
    public event EventHandler<IDictionary<string, object>> Error;

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

    /// <summary>
    /// Gets the current state of the ParseLiveQueryController. This property indicates
    /// whether the controller is in a Closed, Connecting, or Connected state.
    /// </summary>
    /// <remarks>
    /// - `Closed`: Indicates that the controller is not connected.
    /// - `Connecting`: Indicates that a connection attempt is in progress.
    /// - `Connected`: Indicates that the controller is actively connected.
    /// This property is updated based on the controller's connection lifecycle events,
    /// such as when a connection is established or closed, or when an error occurs.
    /// </remarks>
    public ParseLiveQueryState State { get; private set; }
    ArrayList SubscriptionIds { get; }

    CancellationTokenSource ConnectionSignal { get; set; }
    private IDictionary<int, CancellationTokenSource> SubscriptionSignals { get; } = new Dictionary<int, CancellationTokenSource> { };
    private IDictionary<int, CancellationTokenSource> UnsubscriptionSignals { get; } = new Dictionary<int, CancellationTokenSource> { };
    private IDictionary<int, CancellationTokenSource> SubscriptionUpdateSignals { get; } = new Dictionary<int, CancellationTokenSource> { };

    private IDictionary<int, ParseLiveQuerySubscription> Subscriptions { get; set; } = new Dictionary<int, ParseLiveQuerySubscription> { };

    public ParseLiveQueryController(IWebSocketClient webSocketClient)
    {
        WebSocketClient = webSocketClient;
        SubscriptionIds = new ArrayList();
        State = ParseLiveQueryState.Closed;
    }

    private void ProcessMessage(IDictionary<string, object> message)
    {
        int requestId;
        switch (message["op"])
        {
            case "connected":
                State = ParseLiveQueryState.Connected;
                ConnectionSignal?.Cancel();
                // Connected?.Invoke(this, EventArgs.Empty);
                break;

            case "subscribed":
                requestId = Convert.ToInt32(message["requestId"]);
                SubscriptionIds.Add(requestId);
                if (SubscriptionSignals.TryGetValue(requestId, out CancellationTokenSource subscriptionSignal))
                {
                    subscriptionSignal?.Cancel();
                }
                // Subscribed?.Invoke(this, requestId);
                break;

            // TODO subscription update case

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
                requestId = Convert.ToInt32(message["requestId"]);
                if (Subscriptions.TryGetValue(requestId, out ParseLiveQuerySubscription subscription))
                {
                    subscription.OnCreate(message);
                }
                break;

            case "enter":
                requestId = Convert.ToInt32(message["requestId"]);
                if (Subscriptions.TryGetValue(requestId, out subscription))
                {
                    subscription.OnEnter(message);
                }
                break;

            case "update":
                requestId = Convert.ToInt32(message["requestId"]);
                if (Subscriptions.TryGetValue(requestId, out subscription))
                {
                    subscription.OnUpdate(message);
                }
                break;

            case "leave":
                requestId = Convert.ToInt32(message["requestId"]);
                if (Subscriptions.TryGetValue(requestId, out subscription))
                {
                    subscription.OnLeave(message);
                }
                break;

            case "delete":
                requestId = Convert.ToInt32(message["requestId"]);
                if (Subscriptions.TryGetValue(requestId, out subscription))
                {
                    subscription.OnDelete(message);
                }
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

    private async Task SendMessage(IDictionary<string, object> message, CancellationToken cancellationToken)
    {
        await WebSocketClient.SendAsync(JsonUtilities.Encode(AppendSessionToken(message)), cancellationToken);
    }

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

    private void WebSocketClientOnMessageReceived(object sender, string e)
        => ProcessMessage(JsonUtilities.Parse(e) as IDictionary<string, object>);

    /// <summary>
    /// Establishes a connection to the live query server asynchronously. This method initiates the connection process,
    /// manages connection states, and handles any timeout scenarios if the connection cannot be established within the specified duration.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the connection process. If the token is triggered,
    /// the connection process will be terminated.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous connection operation. If the connection is successful,
    /// the task will complete when the connection is established. In the event of a timeout or error,
    /// it will throw the appropriate exception.
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
        else if (State == ParseLiveQueryState.Connecting)
        {
            if (ConnectionSignal is not null)
            {
                if (!ConnectionSignal.Token.WaitHandle.WaitOne(TimeOut))
                {
                    throw new TimeoutException();
                }
            }
        }
    }

    /// <summary>
    /// Subscribes to a live query, enabling real-time updates for the specified query object.
    /// This method sends a subscription request to the live query server and manages the lifecycle of the subscription.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the ParseObject associated with the live query.
    /// </typeparam>
    /// <param name="liveQuery">
    /// The live query instance to subscribe to. It contains details about the query and its parameters.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests. It allows the operation to be canceled if requested.
    /// </param>
    /// <returns>
    /// An object representing the active subscription for the specified query, enabling interaction with the subscribed events and updates.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to subscribe while the live query connection is in a closed state.
    /// </exception>
    /// <exception cref="TimeoutException">
    /// Thrown when the subscription request times out before receiving confirmation from the server.
    /// </exception>
    public async Task<IParseLiveQuerySubscription> SubscribeAsync<T>(ParseLiveQuery<T> liveQuery, CancellationToken cancellationToken = default) where T : ParseObject
    {
        if (State == ParseLiveQueryState.Closed)
        {
            throw new InvalidOperationException("Cannot subscribe to a live query when the connection is closed.");
        }

        int requestId = ++LastRequestId;
        Dictionary<string, object> message = new Dictionary<string, object>
        {
            { "op", "subscribe" },
            { "requestId", requestId },
            { "query", liveQuery.BuildParameters(true) }
        };
        await SendMessage(message, cancellationToken);
        CancellationTokenSource completionSignal = new CancellationTokenSource();
        SubscriptionSignals.Add(requestId, completionSignal);
        bool signalReceived = completionSignal.Token.WaitHandle.WaitOne(TimeOut);
        SubscriptionSignals.Remove(requestId);
        completionSignal.Dispose();
        if (signalReceived)
        {
            ParseLiveQuerySubscription subscription = new ParseLiveQuerySubscription(liveQuery.Services, requestId);
            Subscriptions.Add(requestId, subscription);
            return subscription;
        }
        throw new TimeoutException();
    }

    /// <summary>
    /// Updates an active subscription by sending an "update" operation to the live query server.
    /// This method modifies the parameters of an existing subscription for a specific query.
    /// </summary>
    /// <param name="liveQuery">
    /// The live query object that holds the query parameters to be updated.
    /// </param>
    /// <param name="requestId">
    /// The unique identifier of the subscription to update.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests, allowing the operation to be cancelled before completion.
    /// </param>
    /// <typeparam name="T">
    /// The type of the ParseObject that the query targets.
    /// </typeparam>
    /// <returns>
    /// A task that represents the asynchronous operation of updating the subscription.
    /// </returns>
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

    /// <summary>
    /// Unsubscribes from a live query subscription associated with the given request identifier.
    /// </summary>
    /// <param name="requestId">
    /// The unique identifier of the subscription to unsubscribe from.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the unsubscription operation before completion.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous unsubscription operation.
    /// </returns>
    /// <exception cref="TimeoutException">
    /// Thrown if the unsubscription process does not complete within the specified timeout period.
    /// </exception>
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
    /// Closes the live query connection, resets the state to closed, and clears all active subscriptions and signals.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests while closing the connection.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation for closing the live query connection.
    /// </returns>
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        await WebSocketClient.CloseAsync(cancellationToken);
        State = ParseLiveQueryState.Closed;
        SubscriptionSignals.Clear();
        UnsubscriptionSignals.Clear();
        SubscriptionUpdateSignals.Clear();
        Subscriptions.Clear();
    }
}