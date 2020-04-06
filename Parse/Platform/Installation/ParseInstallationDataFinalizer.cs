using System;
using System.Threading.Tasks;

namespace Parse.Push.Internal
{
    /// <summary>
    /// Controls the device information.
    /// </summary>
    public class ParseInstallationDataFinalizer : IParseInstallationDataFinalizer
    {
        public Task FinalizeAsync(ParseInstallation installation) => Task.FromResult<object>(null);

        public void Initialize() { }
    }
}