using System;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.LiveQueries;
using Parse.Abstractions.Platform.Objects;

namespace Parse.Platform.LiveQueries;

/// <summary>
/// Represents a subscription to updates for a LiveQuery in a Parse Server. Provides hooks for handling
/// various events such as creation, update, deletion, entering, and leaving of objects that match the query.
/// </summary>
public class ParseLiveQuerySubscription<T> : IParseLiveQuerySubscription where T : ParseObject
{
    string ClassName { get; }
    IServiceHub Services { get; }

    private int RequestId { get; set; }

    /// <summary>
    /// Represents the Create event for a live query subscription.
    /// This event is triggered when a new object matching the subscription's query is created.
    /// </summary>
    public event EventHandler<ParseLiveQueryEventArgs> Create;

    /// <summary>
    /// Represents the Enter event for a live query subscription.
    /// This event is triggered when an object that did not previously match the query (and was thus not part of the subscription)
    /// starts matching the query, typically due to an update.
    /// </summary>
    public event EventHandler<ParseLiveQueryDualEventArgs> Enter;

    /// <summary>
    /// Represents the Update event for a live query subscription.
    /// This event is triggered when an existing object matching the subscription's query is updated.
    /// </summary>
    public event EventHandler<ParseLiveQueryDualEventArgs> Update;

    /// <summary>
    /// Represents the Leave event for a live query subscription.
    /// This event is triggered when an object that previously matched the subscription's query
    /// no longer matches the criteria and is removed.
    /// </summary>
    public event EventHandler<ParseLiveQueryDualEventArgs> Leave;

    /// <summary>
    /// Represents the Delete event for a live query subscription.
    /// This event is triggered when an object matching the subscription's query is deleted.
    /// </summary>
    public event EventHandler<ParseLiveQueryEventArgs> Delete;

    /// <summary>
    /// Represents a subscription to a live query, allowing the client to receive real-time event notifications
    /// from the Parse Live Query server for a specified query. This class is responsible for handling events
    /// such as object creation, updates, deletions, and entering or leaving a query's result set.
    /// </summary>
    public ParseLiveQuerySubscription(IServiceHub serviceHub, string className, int requestId)
    {
        Services = serviceHub;
        ClassName = className;
        RequestId = requestId;
    }

    /// <summary>
    /// Updates the current live query subscription with new query parameters,
    /// effectively modifying the subscription to reflect the provided live query.
    /// This allows adjustments to the filter or watched keys without unsubscribing
    /// and re-subscribing.
    /// </summary>
    /// <typeparam name="T1">The type of the ParseObject associated with the subscription.</typeparam>
    /// <param name="liveQuery">The updated live query containing new parameters that
    /// will replace the existing ones for this subscription.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. If triggered,
    /// the update process will be halted.</param>
    /// <returns>A task that represents the asynchronous operation of updating
    /// the subscription with the new query parameters.</returns>
    public async Task UpdateAsync<T1>(ParseLiveQuery<T1> liveQuery, CancellationToken cancellationToken = default) where T1 : ParseObject =>
        await Services.LiveQueryController.UpdateSubscriptionAsync(liveQuery, RequestId, CancellationToken.None);

    /// <summary>
    /// Cancels the current live query subscription by unsubscribing from the Parse Live Query server.
    /// This ensures that the client will no longer receive real-time updates or notifications
    /// associated with this subscription.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. If triggered, the cancellation process will halt.</param>
    /// <returns>A task that represents the asynchronous operation of canceling the subscription.</returns>
    public async Task CancelAsync(CancellationToken cancellationToken = default) =>
        await Services.LiveQueryController.UnsubscribeAsync(RequestId, CancellationToken.None);

    /// <summary>
    /// Handles the creation event for an object that matches the subscription's query.
    /// Invokes the Create event with the parsed object details contained within the provided object state.
    /// </summary>
    /// <param name="objectState">
    /// The state of the object that triggered the creation event, containing its data and metadata.
    /// </param>
    public void OnCreate(IObjectState objectState) =>
        Create?.Invoke(this, new ParseLiveQueryEventArgs(Services.GenerateObjectFromState<T>(objectState, ClassName)));

    /// <summary>
    /// Handles the event when an object enters the result set of a live query subscription. This occurs when an
    /// object begins to satisfy the query conditions.
    /// </summary>
    /// <param name="objectState">The current state of the object that has entered the query result set.</param>
    /// <param name="originalState">The original state of the object before entering the query result set.</param>
    public void OnEnter(IObjectState objectState, IObjectState originalState) =>
        Enter?.Invoke(this, new ParseLiveQueryDualEventArgs(
            Services.GenerateObjectFromState<T>(objectState, ClassName),
            Services.GenerateObjectFromState<T>(originalState, ClassName)));

    /// <summary>
    /// Handles the update event for objects subscribed to the Live Query. This method triggers the Update
    /// event, providing the updated object and its original state.
    /// </summary>
    /// <param name="objectState">The new state of the object after the update.</param>
    /// <param name="originalState">The original state of the object before the update.</param>
    public void OnUpdate(IObjectState objectState, IObjectState originalState) =>
        Update?.Invoke(this, new ParseLiveQueryDualEventArgs(
            Services.GenerateObjectFromState<T>(objectState, ClassName),
            Services.GenerateObjectFromState<T>(originalState, ClassName)));

    /// <summary>
    /// Handles the event when an object leaves the result set of the live query subscription.
    /// This method triggers the <see cref="Leave"/> event to notify that an object has
    /// transitioned out of the query's result set.
    /// </summary>
    /// <param name="objectState">The state of the object that left the result set.</param>
    /// <param name="originalState">The original state of the object before it left the result set.</param>
    public void OnLeave(IObjectState objectState, IObjectState originalState) =>
        Leave?.Invoke(this, new ParseLiveQueryDualEventArgs(
            Services.GenerateObjectFromState<T>(objectState, ClassName),
            Services.GenerateObjectFromState<T>(originalState, ClassName)));

    /// <summary>
    /// Handles the "delete" event for a live query subscription, triggered when an object is removed
    /// from the query's result set. This method processes the event by invoking the associated
    /// delete event handler, if subscribed, with the relevant object data.
    /// </summary>
    /// <param name="objectState">The state information of the object that was deleted.</param>
    public void OnDelete(IObjectState objectState) =>
        Delete?.Invoke(this, new ParseLiveQueryEventArgs(Services.GenerateObjectFromState<T>(objectState, ClassName)));
}