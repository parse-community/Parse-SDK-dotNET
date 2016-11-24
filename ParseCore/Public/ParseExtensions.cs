// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using LeanCloud.Core.Internal;
using LeanCloud.Common.Internal;

namespace LeanCloud {
  /// <summary>
  /// Provides convenience extension methods for working with collections
  /// of ParseObjects so that you can easily save and fetch them in batches.
  /// </summary>
  public static class AVExtensions {
    /// <summary>
    /// Saves all of the ParseObjects in the enumeration. Equivalent to
    /// calling <see cref="ParseObject.SaveAllAsync{T}(IEnumerable{T})"/>.
    /// </summary>
    /// <param name="objects">The objects to save.</param>
    public static Task SaveAllAsync<T>(this IEnumerable<T> objects) where T : AVObject {
      return AVObject.SaveAllAsync(objects);
    }

    /// <summary>
    /// Saves all of the ParseObjects in the enumeration. Equivalent to
    /// calling
    /// <see cref="ParseObject.SaveAllAsync{T}(IEnumerable{T}, CancellationToken)"/>.
    /// </summary>
    /// <param name="objects">The objects to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static Task SaveAllAsync<T>(
        this IEnumerable<T> objects, CancellationToken cancellationToken) where T : AVObject {
      return AVObject.SaveAllAsync(objects, cancellationToken);
    }

    /// <summary>
    /// Fetches all of the objects in the enumeration. Equivalent to
    /// calling <see cref="ParseObject.FetchAllAsync{T}(IEnumerable{T})"/>.
    /// </summary>
    /// <param name="objects">The objects to save.</param>
    public static Task<IEnumerable<T>> FetchAllAsync<T>(this IEnumerable<T> objects)
      where T : AVObject {
      return AVObject.FetchAllAsync(objects);
    }

    /// <summary>
    /// Fetches all of the objects in the enumeration. Equivalent to
    /// calling
    /// <see cref="ParseObject.FetchAllAsync{T}(IEnumerable{T}, CancellationToken)"/>.
    /// </summary>
    /// <param name="objects">The objects to fetch.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static Task<IEnumerable<T>> FetchAllAsync<T>(
        this IEnumerable<T> objects, CancellationToken cancellationToken)
      where T : AVObject {
      return AVObject.FetchAllAsync(objects, cancellationToken);
    }

    /// <summary>
    /// Fetches all of the objects in the enumeration that don't already have
    /// data. Equivalent to calling
    /// <see cref="ParseObject.FetchAllIfNeededAsync{T}(IEnumerable{T})"/>.
    /// </summary>
    /// <param name="objects">The objects to fetch.</param>
    public static Task<IEnumerable<T>> FetchAllIfNeededAsync<T>(
        this IEnumerable<T> objects)
      where T : AVObject {
      return AVObject.FetchAllIfNeededAsync(objects);
    }

    /// <summary>
    /// Fetches all of the objects in the enumeration that don't already have
    /// data. Equivalent to calling
    /// <see cref="ParseObject.FetchAllIfNeededAsync{T}(IEnumerable{T}, CancellationToken)"/>.
    /// </summary>
    /// <param name="objects">The objects to fetch.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static Task<IEnumerable<T>> FetchAllIfNeededAsync<T>(
        this IEnumerable<T> objects, CancellationToken cancellationToken)
      where T : AVObject {
      return AVObject.FetchAllIfNeededAsync(objects, cancellationToken);
    }

    /// <summary>
    /// Constructs a query that is the or of the given queries.
    /// </summary>
    /// <typeparam name="T">The type of AVObject being queried.</typeparam>
    /// <param name="source">An initial query to 'or' with additional queries.</param>
    /// <param name="queries">The list of ParseQueries to 'or' together.</param>
    /// <returns>A query that is the or of the given queries.</returns>
    public static AVQuery<T> Or<T>(this AVQuery<T> source, params AVQuery<T>[] queries)
        where T : AVObject {
      return AVQuery<T>.Or(queries.Concat(new[] { source }));
    }

    /// <summary>
    /// Fetches this object with the data from the server.
    /// </summary>
    public static Task<T> FetchAsync<T>(this T obj) where T : AVObject {
      return obj.FetchAsyncInternal(CancellationToken.None).OnSuccess(t => (T)t.Result);
    }

    /// <summary>
    /// Fetches this object with the data from the server.
    /// </summary>
    /// <param name="obj">The AVObject to fetch.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static Task<T> FetchAsync<T>(this T obj, CancellationToken cancellationToken)
        where T : AVObject {
      return obj.FetchAsyncInternal(cancellationToken).OnSuccess(t => (T)t.Result);
    }

    /// <summary>
    /// If this AVObject has not been fetched (i.e. <see cref="ParseObject.IsDataAvailable"/> returns
    /// false), fetches this object with the data from the server.
    /// </summary>
    /// <param name="obj">The AVObject to fetch.</param>
    public static Task<T> FetchIfNeededAsync<T>(this T obj) where T : AVObject {
      return obj.FetchIfNeededAsyncInternal(CancellationToken.None).OnSuccess(t => (T)t.Result);
    }

    /// <summary>
    /// If this AVObject has not been fetched (i.e. <see cref="ParseObject.IsDataAvailable"/> returns
    /// false), fetches this object with the data from the server.
    /// </summary>
    /// <param name="obj">The AVObject to fetch.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static Task<T> FetchIfNeededAsync<T>(this T obj, CancellationToken cancellationToken)
        where T : AVObject {
      return obj.FetchIfNeededAsyncInternal(cancellationToken).OnSuccess(t => (T)t.Result);
    }
  }
}
