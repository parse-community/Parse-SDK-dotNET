using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Sessions;
using Parse.Infrastructure.Utilities;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure.Execution;
using Parse.Infrastructure.Data;

namespace Parse.Platform.Sessions;

public class ParseSessionController : IParseSessionController
{
    IParseCommandRunner CommandRunner { get; }

    IParseDataDecoder Decoder { get; }

    public ParseSessionController(IParseCommandRunner commandRunner, IParseDataDecoder decoder) => (CommandRunner, Decoder) = (commandRunner, decoder);

    public async Task<IObjectState> GetSessionAsync(string sessionToken, IServiceHub serviceHub, CancellationToken cancellationToken = default)
    {
        var result = await CommandRunner.RunCommandAsync(
            new ParseCommand("sessions/me", method: "GET", sessionToken: sessionToken, data: null),
            cancellationToken: cancellationToken
        );

        return ParseObjectCoder.Instance.Decode(result.Item2, Decoder, serviceHub);
    }


    public Task RevokeAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        return CommandRunner
            .RunCommandAsync(new ParseCommand("logout", method: "POST", sessionToken: sessionToken, data: new Dictionary<string, object> { }), cancellationToken: cancellationToken);
    }

    public async Task<IObjectState> UpgradeToRevocableSessionAsync(
       string sessionToken,
       IServiceHub serviceHub,
       CancellationToken cancellationToken = default)
    {
        var command = new ParseCommand(
            "upgradeToRevocableSession",
            method: "POST",
            sessionToken: sessionToken,
            data: new Dictionary<string, object>()
        );

        var response = await CommandRunner.RunCommandAsync(command,null,null, cancellationToken).ConfigureAwait(false);
        var decoded = ParseObjectCoder.Instance.Decode(response.Item2, Decoder, serviceHub);

        return decoded;
    }


    public bool IsRevocableSessionToken(string sessionToken)
    {
        return sessionToken.Contains("r:");
    }
}
