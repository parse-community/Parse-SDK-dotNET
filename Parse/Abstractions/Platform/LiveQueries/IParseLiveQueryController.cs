using System.Threading;
using System.Threading.Tasks;

namespace Parse.Abstractions.Platform.LiveQueries;

/// <summary>
/// Defines an interface for managing LiveQuery connections, subscriptions, and updates
/// in a Parse Server environment.
/// </summary>
public interface IParseLiveQueryController
{
    Task ConnectAsync(CancellationToken cancellationToken = default);

    Task<IParseLiveQuerySubscription> SubscribeAsync<T>(ParseLiveQuery<T> liveQuery, CancellationToken cancellationToken = default) where T : ParseObject;

    Task UpdateSubscriptionAsync<T>(ParseLiveQuery<T> liveQuery, int requestId, CancellationToken cancellationToken = default) where T : ParseObject;

    Task UnsubscribeAsync(int requestId, CancellationToken cancellationToken = default);

    Task CloseAsync(CancellationToken cancellationToken = default);
}