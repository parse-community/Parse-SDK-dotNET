using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;

namespace Parse.Abstractions.Platform.Cloud
{
    public interface IParseCloudCodeController
    {
        Task<T> CallFunctionAsync<T>(string name, IDictionary<string, object> parameters, string sessionToken, IServiceHub serviceHub, CancellationToken cancellationToken = default);
    }
}
