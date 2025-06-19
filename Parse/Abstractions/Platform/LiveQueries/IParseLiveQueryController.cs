using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Platform.Objects;

namespace Parse.Abstractions.Platform.LiveQueries;

public interface IParseLiveQueryController
{
    Task ConnectAsync(CancellationToken cancellationToken = default);

    Task<int> SubscribeAsync<T>(ParseLiveQuery<T> liveQuery, CancellationToken cancellationToken = default) where T : ParseObject;

    Task UpdateSubscriptionAsync<T>(ParseLiveQuery<T> liveQuery, int requestId, CancellationToken cancellationToken = default) where T : ParseObject;

    Task UnsubscribeAsync(int requestId, CancellationToken cancellationToken = default);

    Task CloseAsync(CancellationToken cancellationToken = default);
}