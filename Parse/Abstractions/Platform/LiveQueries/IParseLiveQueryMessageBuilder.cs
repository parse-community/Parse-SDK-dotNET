using System.Collections.Generic;
using System.Threading.Tasks;

namespace Parse.Abstractions.Platform.LiveQueries;

/// <summary>
/// Defines methods for building messages used to communicate with the Parse Live Query server.
/// </summary>
public interface IParseLiveQueryMessageBuilder
{
    /// <summary>
    /// Builds a message to initiate a connection to the Parse Live Query server.
    /// </summary>
    Task<IDictionary<string, object>> BuildConnectMessage();

    /// <summary>
    /// Builds a message to subscribe to a live query with the specified request ID and query parameters.
    /// <param name="requestId">The unique identifier for the subscription request.</param>
    /// <param name="liveQuery">The live query instance containing the query parameters.</param>
    /// </summary>
    Task<IDictionary<string, object>> BuildSubscribeMessage<T>(int requestId, ParseLiveQuery<T> liveQuery) where T : ParseObject;

    /// <summary>
    /// Builds a message to update a subscription to a live query with the specified request ID and query parameters.
    /// <param name="requestId">The unique identifier for the subscription request.</param>
    /// <param name="liveQuery">The live query instance containing the query parameters.</param>
    /// </summary>
    Task<IDictionary<string, object>> BuildUpdateSubscriptionMessage<T>(int requestId, ParseLiveQuery<T> liveQuery) where T : ParseObject;

    /// <summary>
    /// Builds a message to unsubscribe from a live query with the specified request ID.
    /// <param name="requestId">The unique identifier for the subscription request to be cancelled.</param>
    /// </summary>
    IDictionary<string, object> BuildUnsubscribeMessage(int requestId);
}
