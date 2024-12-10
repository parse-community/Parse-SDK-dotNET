using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure.Control;
using Parse.Abstractions.Infrastructure;

namespace Parse.Abstractions.Platform.Objects;

public interface IParseObjectController
{
    Task<IObjectState> FetchAsync(IObjectState state, string sessionToken, IServiceHub serviceHub, CancellationToken cancellationToken = default);

    Task<IObjectState> SaveAsync(IObjectState state, IDictionary<string, IParseFieldOperation> operations, string sessionToken, IServiceHub serviceHub, CancellationToken cancellationToken = default);

    Task<IEnumerable<Task<IObjectState>>> SaveAllAsync(IEnumerable<IObjectState> states, IEnumerable<IDictionary<string, IParseFieldOperation>> operationsList, string sessionToken, IServiceHub serviceHub, CancellationToken cancellationToken = default);

    Task DeleteAsync(IObjectState state, string sessionToken, CancellationToken cancellationToken = default);

    IEnumerable<Task> DeleteAllAsync(IEnumerable<IObjectState> states, string sessionToken, CancellationToken cancellationToken = default);
}
