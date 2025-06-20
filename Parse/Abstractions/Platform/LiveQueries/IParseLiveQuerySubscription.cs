using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Parse.Abstractions.Platform.LiveQueries;

/// <summary>
/// Represents a live query subscription that is used with Parse's Live Query service.
/// It allows real-time monitoring and event handling for object changes that match
/// a specified query.
/// </summary>
public interface IParseLiveQuerySubscription
{
    /// <summary>
    /// Represents the Create event for a live query subscription.
    /// This event is triggered when a new object matching the subscription's query is created.
    /// </summary>
    public event EventHandler<IDictionary<string, object>> Create;

    /// <summary>
    /// Represents the Enter event for a live query subscription.
    /// This event is triggered when an object that did not previously match the query (and was thus not part of the subscription)
    /// starts matching the query, typically due to an update.
    /// </summary>
    public event EventHandler<IDictionary<string, object>> Enter;

    /// <summary>
    /// Represents the Update event for a live query subscription.
    /// This event is triggered when an existing object matching the subscription's query is updated.
    /// </summary>
    public event EventHandler<IDictionary<string, object>> Update;

    /// <summary>
    /// Represents the Leave event for a live query subscription.
    /// This event is triggered when an object that previously matched the subscription's query
    /// no longer matches the criteria and is removed.
    /// </summary>
    public event EventHandler<IDictionary<string, object>> Leave;

    /// <summary>
    /// Represents the Delete event for a live query subscription.
    /// This event is triggered when an object matching the subscription's query is deleted.
    /// </summary>
    public event EventHandler<IDictionary<string, object>> Delete;

    /// <summary>
    /// Updates the current live query subscription with new query parameters,
    /// effectively modifying the subscription to reflect the provided live query.
    /// This allows adjustments to the filter or watched keys without unsubscribing
    /// and re-subscribing.
    /// </summary>
    /// <typeparam name="T">The type of the ParseObject associated with the subscription.</typeparam>
    /// <param name="liveQuery">The updated live query containing new parameters that
    /// will replace the existing ones for this subscription.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. If triggered,
    /// the update process will be halted.</param>
    /// <returns>A task that represents the asynchronous operation of updating
    /// the subscription with the new query parameters.</returns>
    Task UpdateAsync<T>(ParseLiveQuery<T> liveQuery, CancellationToken cancellationToken = default) where T : ParseObject;

    /// <summary>
    /// Cancels the current live query subscription by unsubscribing from the Parse Live Query server.
    /// This ensures that the client will no longer receive real-time updates or notifications
    /// associated with this subscription.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. If triggered, the cancellation process will halt.</param>
    /// <returns>A task that represents the asynchronous operation of canceling the subscription.</returns>
    Task CancelAsync(CancellationToken cancellationToken = default);
}