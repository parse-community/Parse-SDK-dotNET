using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Platform.Configuration;

namespace Parse;

public static class ConfigurationServiceExtensions
{
    public static ParseConfiguration BuildConfiguration(this IServiceHub serviceHub, IDictionary<string, object> configurationData)
    {
        return ParseConfiguration.Create(configurationData, serviceHub.Decoder, serviceHub);
    }

    public static ParseConfiguration BuildConfiguration(this IParseDataDecoder dataDecoder, IDictionary<string, object> configurationData, IServiceHub serviceHub)
    {
        return ParseConfiguration.Create(configurationData, dataDecoder, serviceHub);
    }

#pragma warning disable CS1030 // #warning directive
#warning Investigate if these methods which simply block a thread waiting for an asynchronous process to complete should be eliminated.

    /// <summary>
    /// Gets the latest fetched ParseConfig.
    /// </summary>
    /// <returns>ParseConfig object</returns>
    public static async Task<ParseConfiguration> GetCurrentConfiguration(this IServiceHub serviceHub)
#pragma warning restore CS1030 // #warning directive
    {
        ParseConfiguration parseConfig = await serviceHub.ConfigurationController.CurrentConfigurationController.GetCurrentConfigAsync(serviceHub);

        return parseConfig;
    }

    internal static void ClearCurrentConfig(this IServiceHub serviceHub)
    {
        _ = serviceHub.ConfigurationController.CurrentConfigurationController.ClearCurrentConfigAsync();
    }

    internal static void ClearCurrentConfigInMemory(this IServiceHub serviceHub)
    {
        _ =  serviceHub.ConfigurationController.CurrentConfigurationController.ClearCurrentConfigInMemoryAsync();
    }

    /// <summary>
    /// Retrieves the ParseConfig asynchronously from the server.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>ParseConfig object that was fetched</returns>
    public static async Task<ParseConfiguration> GetConfigurationAsync(this IServiceHub serviceHub, CancellationToken cancellationToken = default)
    {
        return await serviceHub.ConfigurationController.FetchConfigAsync(await serviceHub.GetCurrentSessionToken(), serviceHub, cancellationToken);
    }
}
