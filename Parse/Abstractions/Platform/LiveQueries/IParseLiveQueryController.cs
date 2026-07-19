using System;
using System.Threading;
using System.Threading.Tasks;
using Parse.Platform.LiveQueries;

namespace Parse.Abstractions.Platform.LiveQueries;

/// <summary>
/// Defines an interface for managing LiveQuery connections, subscriptions, and updates
/// in a Parse Server environment.
/// </summary>
public interface IParseLiveQueryController
{
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
    /// Establishes a connection to the live query server asynchronously.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the connection process. If the token is triggered,
    /// the connection process will be terminated.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous connection operation.
    /// </returns>
    /// <exception cref="TimeoutException">
    /// Thrown when the connection request times out before receiving confirmation from the server.
    /// </exception>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to a live query, enabling real-time updates for the specified query object.
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
    Task<IParseLiveQuerySubscription> SubscribeAsync<T>(ParseLiveQuery<T> liveQuery, CancellationToken cancellationToken = default) where T : ParseObject;

    /// <summary>
    /// Updates an active subscription. This method modifies the parameters of an existing subscription for a specific query.
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
    Task UpdateSubscriptionAsync<T>(ParseLiveQuery<T> liveQuery, int requestId, CancellationToken cancellationToken = default) where T : ParseObject;

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
    Task UnsubscribeAsync(int requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes the live query connection asynchronously.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests while closing the live query connection.
    /// If the operation is canceled, the task will terminate early.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation of closing the live query connection.
    /// The task completes when the connection is fully closed and resources are cleaned up.
    /// </returns>
    Task CloseAsync(CancellationToken cancellationToken = default);
}