using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Objects;

namespace Parse.Abstractions.Platform.Sessions;

public interface IParseSessionController
{
    Task<IObjectState> GetSessionAsync(string sessionToken, IServiceHub serviceHub, CancellationToken cancellationToken = default);

    Task RevokeAsync(string sessionToken, CancellationToken cancellationToken = default);

    Task<IObjectState> UpgradeToRevocableSessionAsync(string sessionToken, IServiceHub serviceHub, CancellationToken cancellationToken = default);

    bool IsRevocableSessionToken(string sessionToken);
}
