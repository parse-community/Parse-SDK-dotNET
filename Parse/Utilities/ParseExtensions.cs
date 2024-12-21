using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
namespace Parse;

/// <summary>
/// Provides convenience extension methods for working with collections
/// of ParseObjects so that you can easily save and fetch them in batches.
/// </summary>
/// <summary>
/// Provides convenience extension methods for working with collections
/// of ParseObjects so that you can easily save and fetch them in batches.
/// </summary>
public static class ParseExtensions
{
    /// <summary>
    /// Fetches this object with the data from the server.
    /// </summary>
    /// <param name="obj">The ParseObject to fetch.</param>
    /// <param name="cancellationToken">The cancellation token (optional).</param>
    public static async Task<T> FetchAsync<T>(this T obj, CancellationToken cancellationToken = default) where T : ParseObject
    {
        var result = await obj.FetchAsyncInternal(cancellationToken).ConfigureAwait(false);
        return (T) result;
    }
    /// <summary>
    /// Fetches all objects in the collection from the server.
    /// </summary>
    public static async Task<IEnumerable<T>> FetchAllAsync<T>(this IEnumerable<T> objects, CancellationToken cancellationToken = default) where T : ParseObject
    {
        if (objects == null || !objects.Any()) return objects;

        var result = await Task.WhenAll(objects.Select(obj => obj.FetchAsyncInternal(cancellationToken))).ConfigureAwait(false);
        return result.Cast<T>();
    }

    /// <summary>
    /// If this ParseObject has not been fetched (i.e. <see cref="ParseObject.IsDataAvailable"/> returns
    /// false), fetches this object with the data from the server.
    /// </summary>
    /// <param name="obj">The ParseObject to fetch.</param>
    /// <param name="cancellationToken">The cancellation token (optional).</param>
    public static async Task<T> FetchIfNeededAsync<T>(this T obj, CancellationToken cancellationToken = default) where T : ParseObject
    {
        var result = await obj.FetchIfNeededAsyncInternal(cancellationToken).ConfigureAwait(false);
        return (T) result;
    }

    /// <summary>
    /// Fetches all objects in the collection from the server only if their data is not available.
    /// </summary>
    public static async Task<IEnumerable<T>> FetchAllIfNeededAsync<T>(this IEnumerable<T> objects, CancellationToken cancellationToken = default) where T : ParseObject
    {
        if (objects == null || !objects.Any())
            return objects;

        var result = await Task.WhenAll(objects.Select(obj => obj.FetchIfNeededAsyncInternal(cancellationToken))).ConfigureAwait(false);
        return result.Cast<T>();
    }

}
