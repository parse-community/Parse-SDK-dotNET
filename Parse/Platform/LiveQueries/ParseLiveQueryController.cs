using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Platform.LiveQueries;
using Parse.Infrastructure.Data;
using Parse.Infrastructure.Utilities;

namespace Parse.Platform.LiveQueries;

/// <summary>
/// The ParseLiveQueryController is responsible for managing live query subscriptions, maintaining a connection
/// to the Parse LiveQuery server, and handling real-time updates from the server.
/// </summary>
public class ParseLiveQueryController : IParseLiveQueryController, IDisposable
{
    private IParseDataDecoder Decoder { get; }
    private IWebSocketClient WebSocketClient { get; }

    private int LastRequestId;

    private string ClientId { get; set; }

    private bool disposed;

    /// <summary>
    /// Gets or sets the timeout duration, in milliseconds, used by the ParseLiveQueryController
    /// for various operations, such as establishing a connection or completing a subscription.
    /// </summary>
    /// <remarks>
    /// This property determines the maximum amount of time the controller will wait for an operation
    /// to complete before throwing a <see cref="TimeoutException"/>. It is used in operations such as
    /// - Connecting to the LiveQuery server.
    /// - Subscribing to a query.
    /// - Unsubscribing from a query.
    /// Ensure that the value is configured appropriately to avoid premature timeout errors in network-dependent processes.
    /// </remarks>
    private int TimeOut { get; }

    /// <summary>
    /// Event triggered when an error occurs during the operation of the ParseLiveQueryController.
    /// </summary>
    /// <remarks>
    /// This event provides details about a live query operation failure, such as specific error messages,
    /// error codes, and whether automatic reconnection is recommended.
    /// It is raised in scenarios like:
    /// - Receiving an error response from the LiveQuery server.
    /// - Issues with subscriptions, unsubscriptions, or query updates.
    /// Subscribers to this event can use the provided <see cref="ParseLiveQueryErrorEventArgs"/> to
    /// understand the error and implement appropriate handling mechanisms.
    /// </remarks>
    public event EventHandler<ParseLiveQueryErrorEventArgs> Error;

    /// <summary>
    /// Represents the state of a connection to the Parse LiveQuery server, indicating whether the connection is closed,
    /// in the process of connecting, or fully established.
    /// </summary>
    public enum ParseLiveQueryState
    {
        /// <summary>
        /// Represents the state where the live query connection is closed.
        /// This indicates that any active connection to the live query server
        /// has been terminated, and no data updates are being received.
        /// </summary>
        Closed,

        /// <summary>
        /// Represents the state where the live query connection is in the process of being established.
        /// This indicates that the client is actively attempting to connect to the live query server,
        /// but the connection has not yet been fully established.
        /// </summary>
        Connecting,

        /// <summary>
        /// Represents the state where the live query connection has been successfully established.
        /// This state indicates that the client is actively connected to the Parse LiveQuery server
        /// and is receiving real-time data updates.
        /// </summary>
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
    public ParseLiveQueryState State => _state;
    private volatile ParseLiveQueryState _state;

    TaskCompletionSource ConnectionSignal { get; set; }
    private ConcurrentDictionary<int, TaskCompletionSource> SubscriptionSignals { get; } = new ConcurrentDictionary<int, TaskCompletionSource>();
    private ConcurrentDictionary<int, TaskCompletionSource> UnsubscriptionSignals { get; } = new ConcurrentDictionary<int, TaskCompletionSource>();
    private ConcurrentDictionary<int, IParseLiveQuerySubscription> Subscriptions { get; set; } = new ConcurrentDictionary<int, IParseLiveQuerySubscription>();


    /// <summary>
    /// Initializes a new instance of the <see cref="ParseLiveQueryController"/> class.
    /// </summary>
    /// <param name="timeOut"></param>
    /// <param name="webSocketClient">
    /// The <see cref="IWebSocketClient"/> implementation to use for the live query connection.
    /// </param>
    /// <param name="decoder"></param>
    /// <remarks>
    /// This constructor is used to initialize a new instance of the <see cref="ParseLiveQueryController"/> class
    /// </remarks>
    public ParseLiveQueryController(int timeOut, IWebSocketClient webSocketClient, IParseDataDecoder decoder)
    {
        TimeOut = timeOut;
        WebSocketClient = webSocketClient;
        _state = ParseLiveQueryState.Closed;
        Decoder = decoder;
    }

    private void ProcessMessage(IDictionary<string, object> message)
    {
        if (!message.TryGetValue("op", out object opValue) || opValue is not string op)
        {
            Debug.WriteLine("Missing or invalid operation in message");
            return;
        }

        switch (op)
        {
            case "connected":
                ProcessConnectionMessage(message);
                break;
            case "subscribed": // Response from subscription and subscription update
                ProcessSubscriptionMessage(message);
                break;
            case "unsubscribed":
                ProcessUnsubscriptionMessage(message);
                break;
            case "error":
                ProcessErrorMessage(message);
                break;
            case "create":
                ProcessCreateEventMessage(message);
                break;
            case "enter":
                ProcessEnterEventMessage(message);
                break;
            case "update":
                ProcessUpdateEventMessage(message);
                break;
            case "leave":
                ProcessLeaveEventMessage(message);
                break;
            case "delete":
                ProcessDeleteEventMessage(message);
                break;
            default:
                Debug.WriteLine($"Unknown operation: {message["op"]}");
                break;
        }
    }

    private bool ValidateClientMessage(IDictionary<string, object> message, out int requestId)
    {
        requestId = 0;

        if (!(message.TryGetValue("clientId", out object clientIdObj) &&
              clientIdObj is string clientId && clientId == ClientId))
            return false;

        return message.TryGetValue("requestId", out object requestIdObj) &&
               Int32.TryParse(requestIdObj?.ToString(), out requestId);
    }

    void ProcessDeleteEventMessage(IDictionary<string, object> message)
    {
        if (!ValidateClientMessage(message, out int requestId))
            return;

        if (Subscriptions.TryGetValue(requestId, out IParseLiveQuerySubscription subscription))
        {
            subscription.OnDelete(ParseObjectCoder.Instance.Decode(
                message["object"] as IDictionary<string, object>,
                Decoder,
                ParseClient.Instance.Services));
        }
    }

    void ProcessLeaveEventMessage(IDictionary<string, object> message)
    {
        if (!ValidateClientMessage(message, out int requestId))
            return;

        if (Subscriptions.TryGetValue(requestId, out IParseLiveQuerySubscription subscription))
        {
            subscription.OnLeave(
                ParseObjectCoder.Instance.Decode(
                    message["object"] as IDictionary<string, object>,
                    Decoder,
                    ParseClient.Instance.Services),
                ParseObjectCoder.Instance.Decode(
                    message["original"] as IDictionary<string, object>,
                    Decoder,
                    ParseClient.Instance.Services));
        }
    }

    void ProcessUpdateEventMessage(IDictionary<string, object> message)
    {
        if (!ValidateClientMessage(message, out int requestId))
            return;

        if (Subscriptions.TryGetValue(requestId, out IParseLiveQuerySubscription subscription))
        {
            subscription.OnUpdate(
                ParseObjectCoder.Instance.Decode(
                    message["object"] as IDictionary<string, object>,
                    Decoder,
                    ParseClient.Instance.Services),
                ParseObjectCoder.Instance.Decode(
                    message["original"] as IDictionary<string, object>,
                    Decoder,
                    ParseClient.Instance.Services));
        }
    }

    void ProcessEnterEventMessage(IDictionary<string, object> message)
    {
        if (!ValidateClientMessage(message, out int requestId))
            return;

        if (Subscriptions.TryGetValue(requestId, out IParseLiveQuerySubscription subscription))
        {
            subscription.OnEnter(
                ParseObjectCoder.Instance.Decode(
                    message["object"] as IDictionary<string, object>,
                    Decoder,
                    ParseClient.Instance.Services),
                ParseObjectCoder.Instance.Decode(
                    message["original"] as IDictionary<string, object>,
                    Decoder,
                    ParseClient.Instance.Services));
        }
    }

    void ProcessCreateEventMessage(IDictionary<string, object> message)
    {
        if (!ValidateClientMessage(message, out int requestId))
            return;

        if (Subscriptions.TryGetValue(requestId, out IParseLiveQuerySubscription subscription))
        {
            subscription.OnCreate(ParseObjectCoder.Instance.Decode(
                message["object"] as IDictionary<string, object>,
                Decoder,
                ParseClient.Instance.Services));
        }
    }

    void ProcessErrorMessage(IDictionary<string, object> message)
    {
        if (!(message.TryGetValue("code", out object codeObj) &&
            Int32.TryParse(codeObj?.ToString(), out int code)))
            return;

        if (!(message.TryGetValue("error", out object errorObj) &&
              errorObj is string error))
            return;

        if (!(message.TryGetValue("reconnect", out object reconnectObj) &&
            Boolean.TryParse(reconnectObj?.ToString(), out bool reconnect)))
            return;

        ParseLiveQueryErrorEventArgs errorArgs = new ParseLiveQueryErrorEventArgs(code, error, reconnect);
        Error?.Invoke(this, errorArgs);
    }

    void ProcessUnsubscriptionMessage(IDictionary<string, object> message)
    {
        if (!ValidateClientMessage(message, out int requestId))
            return;

        if (UnsubscriptionSignals.TryGetValue(requestId, out TaskCompletionSource unsubscriptionSign))
        {
            unsubscriptionSign?.TrySetResult();
        }
    }

    void ProcessSubscriptionMessage(IDictionary<string, object> message)
    {
        if (!ValidateClientMessage(message, out int requestId))
            return;

        if (SubscriptionSignals.TryGetValue(requestId, out TaskCompletionSource subscriptionSignal))
        {
            subscriptionSignal?.TrySetResult();
        }
    }

    void ProcessConnectionMessage(IDictionary<string, object> message)
    {
        if (!(message.TryGetValue("clientId", out object clientIdObj) &&
            clientIdObj is string clientId))
            return;

        ClientId = clientId;
        _state = ParseLiveQueryState.Connected;
        ConnectionSignal.TrySetResult();
    }

    private async Task<IDictionary<string, object>> AppendSessionToken(IDictionary<string, object> message)
    {
        string sessionToken = await ParseClient.Instance.Services.GetCurrentSessionToken();
        return sessionToken is null
            ? message
            : message.Concat(new Dictionary<string, object> {
                { "sessionToken", sessionToken }
            }).ToDictionary();
    }

    private async Task SendMessage(IDictionary<string, object> message, CancellationToken cancellationToken) =>
        await WebSocketClient.SendAsync(JsonUtilities.Encode(message), cancellationToken);

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
    {
        object parsed = JsonUtilities.Parse(e);
        if (parsed is IDictionary<string, object> message)
        {
            ProcessMessage(message);
        }
        else
        {
            Debug.WriteLine($"Invalid message format received: {e}");
        }
    }

    /// <summary>
    /// Establishes a connection to the live query server asynchronously.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token used to propagate notification that the operation should be canceled.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    /// <exception cref="TimeoutException">
    /// Thrown if the live query server connection request exceeds the defined timeout.
    /// </exception>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_state == ParseLiveQueryState.Closed)
        {
            _state = ParseLiveQueryState.Connecting;
            await OpenAsync(cancellationToken);
            WebSocketClient.MessageReceived += WebSocketClientOnMessageReceived;
            Dictionary<string, object> message = new Dictionary<string, object>
            {
                { "op", "connect" },
                { "applicationId", ParseClient.Instance.Services.LiveQueryServerConnectionData.ApplicationID },
                { "windowsKey", ParseClient.Instance.Services.LiveQueryServerConnectionData.Key }
            };

            ConnectionSignal = new TaskCompletionSource();
            try
            {
                await SendMessage(await AppendSessionToken(message), cancellationToken);

                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeOut);

                await ConnectionSignal.Task.WaitAsync(cts.Token);
                _state = ParseLiveQueryState.Connected;
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("Live query server connection request has reached timeout");
            }
            finally
            {
                ConnectionSignal = null;
            }
        }
        else if (_state == ParseLiveQueryState.Connecting && ConnectionSignal is not null)
        {
            await ConnectionSignal.Task.WaitAsync(cancellationToken);
        }
    }

    private async Task SendAndWaitForSignalAsync(IDictionary<string, object> message,
        ConcurrentDictionary<int, TaskCompletionSource> signalDictionary,
        int requestId,
        CancellationToken cancellationToken)
    {
        TaskCompletionSource tcs = new TaskCompletionSource();
        signalDictionary.TryAdd(requestId, tcs);

        try
        {
            await SendMessage(message, cancellationToken);

            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeOut);

            await tcs.Task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"Operation timeout for request {requestId}");
        }
        finally
        {
            signalDictionary.TryRemove(requestId, out _);
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
        if (_state == ParseLiveQueryState.Closed)
        {
            throw new InvalidOperationException("Cannot subscribe to a live query when the connection is closed.");
        }

        int requestId = Interlocked.Increment(ref LastRequestId);
        Dictionary<string, object> message = new Dictionary<string, object>
        {
            { "op", "subscribe" },
            { "requestId", requestId },
            { "query", liveQuery.BuildParameters() }
        };
        await SendAndWaitForSignalAsync(await AppendSessionToken(message), SubscriptionSignals, requestId, cancellationToken);
        ParseLiveQuerySubscription<T> subscription = new ParseLiveQuerySubscription<T>(liveQuery.Services, liveQuery.ClassName, requestId);
        Subscriptions.TryAdd(requestId, subscription);
        return subscription;
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
            { "query", liveQuery.BuildParameters() }
        };
        await SendAndWaitForSignalAsync(await AppendSessionToken(message), SubscriptionSignals, requestId, cancellationToken);
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
        await SendAndWaitForSignalAsync(message, UnsubscriptionSignals, requestId, cancellationToken);
        Subscriptions.TryRemove(requestId, out _);
    }

    /// <summary>
    /// Closes the live query connection, resets the state to close, and clears all active subscriptions and signals.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests while closing the connection.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation for closing the live query connection.
    /// </returns>
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        WebSocketClient.MessageReceived -= WebSocketClientOnMessageReceived;
        await WebSocketClient.CloseAsync(cancellationToken);
        _state = ParseLiveQueryState.Closed;
        SubscriptionSignals.Clear();
        UnsubscriptionSignals.Clear();
        Subscriptions.Clear();
    }

    /// <summary>
    /// Releases all resources used by the <see cref="ParseLiveQueryController"/> instance.
    /// </summary>
    /// <remarks>
    /// This method is used to clean up resources, such as closing open connections or unsubscribing from events,
    /// and should be called when the instance is no longer needed. After calling this method, the instance
    /// cannot be used unless re-initialized.
    /// </remarks>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the resources used by the <see cref="ParseLiveQueryController"/> instance.
    /// </summary>
    /// <remarks>
    /// This method implements the <see cref="IDisposable"/> interface and is used to clean up any managed or unmanaged
    /// resources used by the <see cref="ParseLiveQueryController"/> instance.
    /// </remarks>
    private void Dispose(bool disposing)
    {
        if (disposed)
            return;
        if (disposing)
        {
            CloseAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
        disposed = true;
    }
}