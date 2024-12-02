using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Configuration;
using Parse.Infrastructure.Utilities;
using Parse;
using Parse.Infrastructure.Execution;

namespace Parse.Platform.Configuration;

/// <summary>
/// Config controller.
/// </summary>
internal class ParseConfigurationController : IParseConfigurationController
{
    private IParseCommandRunner CommandRunner { get; }
    private IParseDataDecoder Decoder { get; }
    public IParseCurrentConfigurationController CurrentConfigurationController { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ParseConfigurationController"/> class.
    /// </summary>
    public ParseConfigurationController(IParseCommandRunner commandRunner, ICacheController storageController, IParseDataDecoder decoder)
    {
        CommandRunner = commandRunner;
        CurrentConfigurationController = new ParseCurrentConfigurationController(storageController, decoder);
        Decoder = decoder;
    }

    public async Task<ParseConfiguration> FetchConfigAsync(string sessionToken, IServiceHub serviceHub, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Fetch configuration via the command runner (returns a Task)
        var commandResult = await CommandRunner.RunCommandAsync(new ParseCommand("config", method: "GET", sessionToken: sessionToken, null, null),null, null).ConfigureAwait(false);

        // Build the configuration using the decoder (assuming BuildConfiguration is async)
        var config = Decoder.BuildConfiguration(commandResult.Item2, serviceHub);

        // Set the current configuration (assuming SetCurrentConfigAsync is async)
        await CurrentConfigurationController.SetCurrentConfigAsync(config).ConfigureAwait(false);

        return config;
    }

}
