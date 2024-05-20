using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;

namespace Parse.Abstractions.Platform.Analytics
{
    /// <summary>
    /// The interface for the Parse Analytics API controller.
    /// </summary>
    public interface IParseAnalyticsController
    {
        /// <summary>
        /// Tracks an event matching the specified details.
        /// </summary>
        /// <param name="name">The name of the event.</param>
        /// <param name="dimensions">The parameters of the event.</param>
        /// <param name="sessionToken">The session token for the event.</param>
        /// <param name="cancellationToken">The asynchonous cancellation token.</param>
        /// <returns>A <see cref="Task"/> that will complete successfully once the event has been set to be tracked.</returns>
        Task TrackEventAsync(string name, IDictionary<string, string> dimensions, string sessionToken, IServiceHub serviceHub, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tracks an app open for the specified event.
        /// </summary>
        /// <param name="pushHash">The hash for the target push notification.</param>
        /// <param name="sessionToken">The token of the current session.</param>
        /// <param name="cancellationToken">The asynchronous cancellation token.</param>
        /// <returns>A <see cref="Task"/> the will complete successfully once app openings for the target push notification have been set to be tracked.</returns>
        Task TrackAppOpenedAsync(string pushHash, string sessionToken, IServiceHub serviceHub, CancellationToken cancellationToken = default);
    }
}
