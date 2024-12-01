using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;

namespace Parse
{
    public static class InstallationServiceExtensions
    {
        /// <summary>
        /// Constructs a <see cref="ParseQuery{ParseInstallation}"/> for ParseInstallations.
        /// </summary>
        /// <remarks>
        /// Only the following types of queries are allowed for installations:
        ///
        /// <code>
        /// query.GetAsync(objectId)
        /// query.WhereEqualTo(key, value)
        /// query.WhereMatchesKeyInQuery&lt;TOther&gt;(key, keyInQuery, otherQuery)
        /// </code>
        ///
        /// You can add additional query conditions, but one of the above must appear as a top-level <c>AND</c>
        /// clause in the query.
        /// </remarks>
        public static ParseQuery<ParseInstallation> GetInstallationQuery(this IServiceHub serviceHub)
        {
            return new ParseQuery<ParseInstallation>(serviceHub);
        }

#pragma warning disable CS1030 // #warning directive
#warning Consider making the following method asynchronous.

        /// <summary>
        /// Gets the ParseInstallation representing this app on this device.
        /// </summary>
        public static ParseInstallation GetCurrentInstallation(this IServiceHub serviceHub)
#pragma warning restore CS1030 // #warning directive
        {
            Task<ParseInstallation> task = serviceHub.CurrentInstallationController.GetAsync(serviceHub);

            // TODO (hallucinogen): this will absolutely break on Unity, but how should we resolve this?
            task.Wait();
            return task.Result;
        }

        internal static void ClearInMemoryInstallation(this IServiceHub serviceHub)
        {
            serviceHub.CurrentInstallationController.ClearFromMemory();
        }
    }
}
