using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Installations;
using Parse.Abstractions.Platform.Push;
using Parse.Infrastructure.Utilities;

namespace Parse.Platform.Push
{
    internal class ParsePushChannelsController : IParsePushChannelsController
    {
        IParseCurrentInstallationController CurrentInstallationController { get; }

        public ParsePushChannelsController(IParseCurrentInstallationController currentInstallationController) => CurrentInstallationController = currentInstallationController;

        public Task SubscribeAsync(IEnumerable<string> channels, IServiceHub serviceHub, CancellationToken cancellationToken = default) => CurrentInstallationController.GetAsync(serviceHub, cancellationToken).OnSuccess(task =>
        {
            task.Result.AddRangeUniqueToList(nameof(channels), channels);
            return task.Result.SaveAsync(cancellationToken);
        }).Unwrap();

        public Task UnsubscribeAsync(IEnumerable<string> channels, IServiceHub serviceHub, CancellationToken cancellationToken = default) => CurrentInstallationController.GetAsync(serviceHub, cancellationToken).OnSuccess(task =>
        {
            task.Result.RemoveAllFromList(nameof(channels), channels);
            return task.Result.SaveAsync(cancellationToken);
        }).Unwrap();
    }
}
