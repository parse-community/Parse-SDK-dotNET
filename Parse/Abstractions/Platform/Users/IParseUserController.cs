using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Objects;

namespace Parse.Abstractions.Platform.Users;

public interface IParseUserController
{
    Task<IObjectState> SignUpAsync(IObjectState state, IDictionary<string, IParseFieldOperation> operations, IServiceHub serviceHub, CancellationToken cancellationToken = default);

    Task<IObjectState> LogInAsync(string username, string password, IServiceHub serviceHub, CancellationToken cancellationToken = default);

    Task<IObjectState> LogInAsync(string authType, IDictionary<string, object> data, IServiceHub serviceHub, CancellationToken cancellationToken = default);

    Task<IObjectState> GetUserAsync(string sessionToken, IServiceHub serviceHub, CancellationToken cancellationToken = default);

    Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default);

    bool RevocableSessionEnabled { get; set; }

}
