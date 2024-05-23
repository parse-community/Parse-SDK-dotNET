using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Parse.Infrastructure.Execution;

namespace Parse.Abstractions.Infrastructure.Execution
{
    public interface IParseCommandRunner
    {
        /// <summary>
        /// Executes <see cref="ParseCommand"/> and convert the result into Dictionary.
        /// </summary>
        /// <param name="command">The command to be run.</param>
        /// <param name="uploadProgress">Upload progress callback.</param>
        /// <param name="downloadProgress">Download progress callback.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns></returns>
        Task<Tuple<HttpStatusCode, IDictionary<string, object>>> RunCommandAsync(ParseCommand command, IProgress<IDataTransferLevel> uploadProgress = null, IProgress<IDataTransferLevel> downloadProgress = null, CancellationToken cancellationToken = default);
    }
}
