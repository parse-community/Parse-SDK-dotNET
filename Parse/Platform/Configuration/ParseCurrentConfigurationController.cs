using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Configuration;
using Parse.Infrastructure.Utilities;

namespace Parse.Platform.Configuration
{
    /// <summary>
    /// Parse current config controller.
    /// </summary>
    internal class ParseCurrentConfigurationController : IParseCurrentConfigurationController
    {
        static string CurrentConfigurationKey { get; } = "CurrentConfig";

        TaskQueue TaskQueue { get; }

        ParseConfiguration CurrentConfiguration { get; set; }

        ICacheController CacheController { get; }

        IParseDataDecoder Decoder { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParseCurrentConfigurationController"/> class.
        /// </summary>
        public ParseCurrentConfigurationController(ICacheController storageController, IParseDataDecoder decoder)
        {
            CacheController = storageController;
            Decoder = decoder;
            TaskQueue = new TaskQueue { };
        }

        public Task<ParseConfiguration> GetCurrentConfigAsync(IServiceHub serviceHub) => TaskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ => CurrentConfiguration is { } ? Task.FromResult(CurrentConfiguration) : CacheController.LoadAsync().OnSuccess(task =>
        {
            task.Result.TryGetValue(CurrentConfigurationKey, out object data);
            return CurrentConfiguration = data is string { } configuration ? Decoder.BuildConfiguration(JsonUtilities.DeserializeJsonText(configuration), serviceHub) : new ParseConfiguration(serviceHub);
        })), CancellationToken.None).Unwrap();

        public Task SetCurrentConfigAsync(ParseConfiguration target) => TaskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ =>
        {
            CurrentConfiguration = target;
            return CacheController.LoadAsync().OnSuccess(task => task.Result.AddAsync(CurrentConfigurationKey, JsonUtilities.SerializeToJsonText(((IJsonConvertible) target).ConvertToJson())));
        }).Unwrap().Unwrap(), CancellationToken.None);

        public Task ClearCurrentConfigAsync() => TaskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ =>
        {
            CurrentConfiguration = null;
            return CacheController.LoadAsync().OnSuccess(task => task.Result.RemoveAsync(CurrentConfigurationKey));
        }).Unwrap().Unwrap(), CancellationToken.None);

        public Task ClearCurrentConfigInMemoryAsync() => TaskQueue.Enqueue(toAwait => toAwait.ContinueWith(_ => CurrentConfiguration = null), CancellationToken.None);
    }
}
