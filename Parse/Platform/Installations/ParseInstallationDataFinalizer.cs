using System.Threading.Tasks;
using Parse.Abstractions.Platform.Installations;

namespace Parse.Platform.Installations
{
    /// <summary>
    /// Controls the device information.
    /// </summary>
    public class ParseInstallationDataFinalizer : IParseInstallationDataFinalizer
    {
        public Task FinalizeAsync(ParseInstallation installation)
        {
            return Task.FromResult<object>(null);
        }

        public void Initialize() { }
    }
}