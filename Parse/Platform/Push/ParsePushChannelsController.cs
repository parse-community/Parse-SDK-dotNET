using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Installations;
using Parse.Abstractions.Platform.Push;
using Parse.Infrastructure.Utilities;

namespace Parse.Platform.Push;
internal class ParsePushChannelsController : IParsePushChannelsController
{
    private IParseCurrentInstallationController CurrentInstallationController { get; }

    public ParsePushChannelsController(IParseCurrentInstallationController currentInstallationController)
    {
        CurrentInstallationController = currentInstallationController;
    }

    public async Task SubscribeAsync(IEnumerable<string> channels, IServiceHub serviceHub, CancellationToken cancellationToken = default)
    {
        var installation = await CurrentInstallationController.GetAsync(serviceHub, cancellationToken).ConfigureAwait(false);
        installation.AddRangeUniqueToList(nameof(channels), channels);
        await installation.SaveAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UnsubscribeAsync(IEnumerable<string> channels, IServiceHub serviceHub, CancellationToken cancellationToken = default)
    {
        var installation = await CurrentInstallationController.GetAsync(serviceHub, cancellationToken).ConfigureAwait(false);
        installation.RemoveAllFromList(nameof(channels), channels);
        await installation.SaveAsync(cancellationToken).ConfigureAwait(false);
    }
}

