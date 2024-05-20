using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;

namespace Parse.Abstractions.Platform.Push
{
    public interface IParsePushChannelsController
    {
        Task SubscribeAsync(IEnumerable<string> channels, IServiceHub serviceHub, CancellationToken cancellationToken);

        Task UnsubscribeAsync(IEnumerable<string> channels, IServiceHub serviceHub, CancellationToken cancellationToken);
    }
}
