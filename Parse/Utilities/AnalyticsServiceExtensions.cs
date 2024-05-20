using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Infrastructure.Utilities;

namespace Parse
{
    /// <summary>
    /// Provides an interface to Parse's logging and analytics backend.
    ///
    /// Methods will return immediately and cache requests (along with timestamps)
    /// to be handled in the background.
    /// </summary>
    public static class AnalyticsServiceExtensions
    {
        /// <summary>
        /// Tracks this application being launched.
        /// </summary>
        /// <returns>An Async Task that can be waited on or ignored.</returns>
        public static Task TrackLaunchAsync(this IServiceHub serviceHub) => TrackLaunchWithPushHashAsync(serviceHub);

        /// <summary>
        /// Tracks the occurrence of a custom event with additional dimensions.
        /// Parse will store a data point at the time of invocation with the
        /// given event name.
        ///
        /// Dimensions will allow segmentation of the occurrences of this
        /// custom event.
        ///
        /// To track a user signup along with additional metadata, consider the
        /// following:
        /// <code>
        /// IDictionary&lt;string, string&gt; dims = new Dictionary&lt;string, string&gt; {
        ///   { "gender", "m" },
        ///   { "source", "web" },
        ///   { "dayType", "weekend" }
        /// };
        /// ParseAnalytics.TrackEventAsync("signup", dims);
        /// </code>
        ///
        /// There is a default limit of 8 dimensions per event tracked.
        /// </summary>
        /// <param name="name">The name of the custom event to report to ParseClient
        /// as having happened.</param>
        /// <returns>An Async Task that can be waited on or ignored.</returns>
        public static Task TrackAnalyticsEventAsync(this IServiceHub serviceHub, string name) => TrackAnalyticsEventAsync(serviceHub, name, default);

        /// <summary>
        /// Tracks the occurrence of a custom event with additional dimensions.
        /// Parse will store a data point at the time of invocation with the
        /// given event name.
        ///
        /// Dimensions will allow segmentation of the occurrences of this
        /// custom event.
        ///
        /// To track a user signup along with additional metadata, consider the
        /// following:
        /// <code>
        /// IDictionary&lt;string, string&gt; dims = new Dictionary&lt;string, string&gt; {
        ///   { "gender", "m" },
        ///   { "source", "web" },
        ///   { "dayType", "weekend" }
        /// };
        /// ParseAnalytics.TrackEventAsync("signup", dims);
        /// </code>
        ///
        /// There is a default limit of 8 dimensions per event tracked.
        /// </summary>
        /// <param name="name">The name of the custom event to report to ParseClient
        /// as having happened.</param>
        /// <param name="dimensions">The dictionary of information by which to
        /// segment this event.</param>
        /// <returns>An Async Task that can be waited on or ignored.</returns>
        public static Task TrackAnalyticsEventAsync(this IServiceHub serviceHub, string name, IDictionary<string, string> dimensions)
        {
            if (name is null || name.Trim().Length == 0)
            {
                throw new ArgumentException("A name for the custom event must be provided.");
            }

            return serviceHub.CurrentUserController.GetCurrentSessionTokenAsync(serviceHub).OnSuccess(task => serviceHub.AnalyticsController.TrackEventAsync(name, dimensions, task.Result, serviceHub)).Unwrap();
        }

        /// <summary>
        /// Private method, used by platform-specific extensions to report an app-open
        /// to the server.
        /// </summary>
        /// <param name="pushHash">An identifying hash for a given push notification,
        /// passed down from the server.</param>
        /// <returns>An Async Task that can be waited on or ignored.</returns>
        static Task TrackLaunchWithPushHashAsync(this IServiceHub serviceHub, string pushHash = null) => serviceHub.CurrentUserController.GetCurrentSessionTokenAsync(serviceHub).OnSuccess(task => serviceHub.AnalyticsController.TrackAppOpenedAsync(pushHash, task.Result, serviceHub)).Unwrap();
    }
}
