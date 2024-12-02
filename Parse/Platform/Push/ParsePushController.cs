using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Platform.Push;
using Parse.Abstractions.Platform.Users;
using Parse.Infrastructure.Execution;
using Parse.Infrastructure.Utilities;

namespace Parse.Platform.Push;
internal class ParsePushController : IParsePushController
{
    private IParseCommandRunner CommandRunner { get; }
    private IParseCurrentUserController CurrentUserController { get; }

    public ParsePushController(IParseCommandRunner commandRunner, IParseCurrentUserController currentUserController)
    {
        CommandRunner = commandRunner;
        CurrentUserController = currentUserController;
    }

    public async Task SendPushNotificationAsync(IPushState state, IServiceHub serviceHub, CancellationToken cancellationToken = default)
    {
        // Fetch the current session token
        var sessionToken = await CurrentUserController.GetCurrentSessionTokenAsync(serviceHub, cancellationToken).ConfigureAwait(false);

        // Create the push command and execute it
        var pushCommand = new ParseCommand(
            "push",
            method: "POST",
            sessionToken: sessionToken,
            data: ParsePushEncoder.Instance.Encode(state));

        await CommandRunner.RunCommandAsync(pushCommand, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
