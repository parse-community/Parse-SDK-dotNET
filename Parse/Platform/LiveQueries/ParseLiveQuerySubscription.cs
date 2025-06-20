using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.LiveQueries;

namespace Parse.Platform.LiveQueries;

/// <summary>
/// Represents a subscription to updates for a LiveQuery in a Parse Server. Provides hooks for handling
/// various events such as creation, update, deletion, entering, and leaving of objects that match the query.
/// </summary>
public class ParseLiveQuerySubscription : IParseLiveQuerySubscription
{

    internal IServiceHub Services { get; }

    private int RequestId { get; set; }

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
    /// Represents a subscription to a live query, allowing the client to receive real-time event notifications
    /// from the Parse Live Query server for a specified query. This class is responsible for handling events
    /// such as object creation, updates, deletions, and entering or leaving a query's result set.
    /// </summary>
    public ParseLiveQuerySubscription(IServiceHub serviceHub, int requestId)
    {
        Services = serviceHub;
        RequestId = requestId;
    }

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
    public async Task UpdateAsync<T>(ParseLiveQuery<T> liveQuery, CancellationToken cancellationToken = default) where T : ParseObject
    {
        await Services.LiveQueryController.UpdateSubscriptionAsync(liveQuery, RequestId, CancellationToken.None);
    }

    /// <summary>
    /// Cancels the current live query subscription by unsubscribing from the Parse Live Query server.
    /// This ensures that the client will no longer receive real-time updates or notifications
    /// associated with this subscription.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. If triggered, the cancellation process will halt.</param>
    /// <returns>A task that represents the asynchronous operation of canceling the subscription.</returns>
    public async Task CancelAsync(CancellationToken cancellationToken = default)
    {
        await Services.LiveQueryController.UnsubscribeAsync(RequestId, CancellationToken.None);
    }

    /// <summary>
    /// Handles invocation of the Create event for the live query subscription, signaling that a new object
    /// has been created and matches the query criteria. This method triggers the associated Create event
    /// to notify any subscribed listeners about the creation event.
    /// </summary>
    /// <param name="data">A dictionary containing the data associated with the created object, typically including
    /// information such as object attributes and metadata.</param>
    public void OnCreate(IDictionary<string, object> data) => Create?.Invoke(this, data);

    /// <summary>
    /// Triggers the Enter event, indicating that an object has entered the result set of the live query.
    /// This generally occurs when a Parse Object that did not previously match the query conditions now does.
    /// </summary>
    /// <param name="data">A dictionary containing the details of the object that triggered the event.</param>
    public void OnEnter(IDictionary<string, object> data) => Enter?.Invoke(this, data);

    /// <summary>
    /// Handles the event triggered when an object in the subscribed live query is updated. This method
    /// invokes the corresponding handler with the provided update data.
    /// </summary>
    /// <param name="data">A dictionary containing the data associated with the update event.
    /// The data typically includes updated fields and their new values.</param>
    public void OnUpdate(IDictionary<string, object> data) => Update?.Invoke(this, data);

    /// <summary>
    /// Triggers the Leave event when an object leaves the query's result set.
    /// This method notifies all registered event handlers, providing the relevant data associated
    /// with the event.
    /// </summary>
    /// <param name="data">A dictionary that contains information about the object leaving the query's result set.</param>
    public void OnLeave(IDictionary<string, object> data) => Leave?.Invoke(this, data);

    /// <summary>
    /// Handles the deletion event triggered by the Parse Live Query server. This method is invoked when an object
    /// that matches the current query result set is deleted, notifying all subscribers of this event.
    /// </summary>
    /// <param name="data">A dictionary containing information about the deleted object and any additional context provided by the server.</param>
    public void OnDelete(IDictionary<string, object> data) => Delete?.Invoke(this, data);
}