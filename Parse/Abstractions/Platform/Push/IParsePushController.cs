using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;

namespace Parse.Abstractions.Platform.Push
{
    public interface IParsePushController
    {
        Task SendPushNotificationAsync(IPushState state, IServiceHub serviceHub, CancellationToken cancellationToken = default);
    }
}
