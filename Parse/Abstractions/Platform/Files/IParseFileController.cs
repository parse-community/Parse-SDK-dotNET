using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Platform.Files;

namespace Parse.Abstractions.Platform.Files
{
    public interface IParseFileController
    {
        Task<FileState> SaveAsync(FileState state, Stream dataStream, string sessionToken, IProgress<IDataTransferLevel> progress, CancellationToken cancellationToken);
    }
}
