using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Configuration;
using Parse.Infrastructure.Utilities;

namespace Parse.Platform.Configuration;

/// <summary>
/// Parse current config controller.
/// </summary>
internal class ParseCurrentConfigurationController : IParseCurrentConfigurationController
{
    private static readonly string CurrentConfigurationKey = "CurrentConfig";

    private ParseConfiguration _currentConfiguration;
    private readonly ICacheController _storageController;
    private readonly IParseDataDecoder _decoder;

    public ParseCurrentConfigurationController(ICacheController storageController, IParseDataDecoder decoder)
    {
        _storageController = storageController;
        _decoder = decoder;
    }

    public async Task<ParseConfiguration> GetCurrentConfigAsync(IServiceHub serviceHub)
    {
        if (_currentConfiguration != null)
            return _currentConfiguration;

        var data = await _storageController.LoadAsync();
        data.TryGetValue(CurrentConfigurationKey, out var storedData);

        _currentConfiguration = storedData is string configString
            ? _decoder.BuildConfiguration(ParseClient.DeserializeJsonString(configString), serviceHub)
            : new ParseConfiguration(serviceHub);

        return _currentConfiguration;
    }

    public async Task SetCurrentConfigAsync(ParseConfiguration target)
    {
        _currentConfiguration = target;

        var data = await _storageController.LoadAsync();
        await data.AddAsync(CurrentConfigurationKey, ParseClient.SerializeJsonString(((IJsonConvertible) target).ConvertToJSON()));
    }

    public async Task ClearCurrentConfigAsync()
    {
        _currentConfiguration = null;

        var data = await _storageController.LoadAsync();
        await data.RemoveAsync(CurrentConfigurationKey);
    }

    public Task ClearCurrentConfigInMemoryAsync() => Task.Run(() => _currentConfiguration = null);
}
