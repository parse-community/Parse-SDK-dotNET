using System.Threading;
using System.Threading.Tasks;
using Parse.Infrastructure.Utilities;

namespace Parse
{
    /// <summary>
    /// Provides convenience extension methods for working with collections
    /// of ParseObjects so that you can easily save and fetch them in batches.
    /// </summary>
    public static class ParseExtensions
    {
        /// <summary>
        /// Fetches this object with the data from the server.
        /// </summary>
        public static Task<T> FetchAsync<T>(this T obj) where T : ParseObject => obj.FetchAsyncInternal(CancellationToken.None).OnSuccess(t => (T) t.Result);

        /// <summary>
        /// Fetches this object with the data from the server.
        /// </summary>
        /// <param name="target">The ParseObject to fetch.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static Task<T> FetchAsync<T>(this T target, CancellationToken cancellationToken) where T : ParseObject => target.FetchAsyncInternal(cancellationToken).OnSuccess(task => (T) task.Result);

        /// <summary>
        /// If this ParseObject has not been fetched (i.e. <see cref="ParseObject.IsDataAvailable"/> returns
        /// false), fetches this object with the data from the server.
        /// </summary>
        /// <param name="obj">The ParseObject to fetch.</param>
        public static Task<T> FetchIfNeededAsync<T>(this T obj) where T : ParseObject => obj.FetchIfNeededAsyncInternal(CancellationToken.None).OnSuccess(t => (T) t.Result);

        /// <summary>
        /// If this ParseObject has not been fetched (i.e. <see cref="ParseObject.IsDataAvailable"/> returns
        /// false), fetches this object with the data from the server.
        /// </summary>
        /// <param name="obj">The ParseObject to fetch.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static Task<T> FetchIfNeededAsync<T>(this T obj, CancellationToken cancellationToken) where T : ParseObject => obj.FetchIfNeededAsyncInternal(cancellationToken).OnSuccess(t => (T) t.Result);
    }
}
