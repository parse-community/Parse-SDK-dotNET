using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;

namespace Parse
{
    /// <summary>
    /// The ParseCloud class provides methods for interacting with Parse Cloud Functions.
    /// </summary>
    /// <example>
    /// For example, this sample code calls the
    /// "validateGame" Cloud Function and calls processResponse if the call succeeded
    /// and handleError if it failed.
    ///
    /// <code>
    /// var result =
    ///     await ParseCloud.CallFunctionAsync&lt;IDictionary&lt;string, object&gt;&gt;("validateGame", parameters);
    /// </code>
    /// </example>
    public static class CloudCodeServiceExtensions
    {
        /// <summary>
        /// Calls a cloud function.
        /// </summary>
        /// <typeparam name="T">The type of data you will receive from the cloud function. This
        /// can be an IDictionary, string, IList, ParseObject, or any other type supported by
        /// ParseObject.</typeparam>
        /// <param name="name">The cloud function to call.</param>
        /// <param name="parameters">The parameters to send to the cloud function. This
        /// dictionary can contain anything that could be passed into a ParseObject except for
        /// ParseObjects themselves.</param>
        /// <returns>The result of the cloud call.</returns>
        public static Task<T> CallCloudCodeFunctionAsync<T>(this IServiceHub serviceHub, string name, IDictionary<string, object> parameters)
        {
            return CallCloudCodeFunctionAsync<T>(serviceHub, name, parameters, CancellationToken.None);
        }

        /// <summary>
        /// Calls a cloud function.
        /// </summary>
        /// <typeparam name="T">The type of data you will receive from the cloud function. This
        /// can be an IDictionary, string, IList, ParseObject, or any other type supported by
        /// ParseObject.</typeparam>
        /// <param name="name">The cloud function to call.</param>
        /// <param name="parameters">The parameters to send to the cloud function. This
        /// dictionary can contain anything that could be passed into a ParseObject except for
        /// ParseObjects themselves.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The result of the cloud call.</returns>
        public static Task<T> CallCloudCodeFunctionAsync<T>(this IServiceHub serviceHub, string name, IDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            return serviceHub.CloudCodeController.CallFunctionAsync<T>(name, parameters, serviceHub.GetCurrentSessionToken(), serviceHub, cancellationToken);
        }
    }
}
