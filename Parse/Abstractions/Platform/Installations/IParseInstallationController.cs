using System;
using System.Threading.Tasks;

namespace Parse.Abstractions.Platform.Installations
{
    public interface IParseInstallationController
    {
        /// <summary>
        /// Sets current <code>installationId</code> and saves it to local storage.
        /// </summary>
        /// <param name="installationId">The <code>installationId</code> to be saved.</param>
        Task SetAsync(Guid? installationId);

        /// <summary>
        /// Gets current <code>installationId</code> from local storage. Generates a none exists.
        /// </summary>
        /// <returns>Current <code>installationId</code>.</returns>
        Task<Guid?> GetAsync();

        /// <summary>
        /// Clears current installationId from memory and local storage.
        /// </summary>
        Task ClearAsync();
    }
}
