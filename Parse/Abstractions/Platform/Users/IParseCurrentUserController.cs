using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Objects;

namespace Parse.Abstractions.Platform.Users;

public interface IParseCurrentUserController : IParseObjectCurrentController<ParseUser>
{
    Task<string> GetCurrentSessionTokenAsync(IServiceHub serviceHub, CancellationToken cancellationToken = default);

    Task LogOutAsync(IServiceHub serviceHub, CancellationToken cancellationToken = default);
}
