using System.Threading.Tasks;

namespace Parse.Abstractions.Platform.Installations
{
    public interface IParseInstallationDataFinalizer
    {
        /// <summary>
        /// Executes platform specific hook that mutate the installation based on
        /// the device platforms.
        /// </summary>
        /// <param name="installation">Installation to be mutated.</param>
        /// <returns></returns>
        Task FinalizeAsync(ParseInstallation installation);

        /// <summary>
        /// Allows an implementation to get static information that needs to be used in <see cref="FinalizeAsync(ParseInstallation)"/>.
        /// </summary>
        void Initialize();
    }
}
