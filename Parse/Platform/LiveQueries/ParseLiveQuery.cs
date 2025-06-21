using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.LiveQueries;
using Parse.Infrastructure.Data;
using Parse.Infrastructure.Utilities;

namespace Parse;

/// <summary>
/// The ParseLiveQuery class allows subscribing to a Query.
/// </summary>
/// <typeparam name="T"></typeparam>
public class ParseLiveQuery<T> where T : ParseObject
{

    /// <summary>
    /// Serialized <see langword="where"/> clauses.
    /// </summary>
    string Filters { get; }

    /// <summary>
    /// Serialized key selections.
    /// </summary>
    ReadOnlyCollection<string> KeySelections { get; }

    /// <summary>
    /// Serialized keys watched.
    /// </summary>
    ReadOnlyCollection<string> KeyWatchers { get; }

    internal string ClassName { get; }

    internal IServiceHub Services { get; }

    private int RequestId = 0;

    public ParseLiveQuery(IServiceHub serviceHub, string className, object filters, IEnumerable<string> selectedKeys = null, IEnumerable<string> watchedKeys = null)
    {
        Services = serviceHub;
        ClassName = className;
        Filters = JsonUtilities.Encode(filters);

        if (selectedKeys is not null)
        {
            KeySelections = new ReadOnlyCollection<string>(selectedKeys.ToList());
        }

        if (watchedKeys is not null)
        {
            KeyWatchers = new ReadOnlyCollection<string>(watchedKeys.ToList());
        }
    }

    /// <summary>
    /// Private constructor for composition of queries. A source query is required,
    /// but the remaining values can be null if they aren't changed in this
    /// composition.
    /// </summary>
    internal ParseLiveQuery(ParseLiveQuery<T> source, IEnumerable<string> watchedKeys = null, Func<IDictionary<string, object>> onCreate = null)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        Services = source.Services;
        ClassName = source.ClassName;
        Filters = source.Filters;
        KeySelections = source.KeySelections;
        KeyWatchers = source.KeyWatchers;

        if (watchedKeys is { })
        {
            KeyWatchers = new ReadOnlyCollection<string>(MergeWatchers(watchedKeys).ToList());
        }
    }

    HashSet<string> MergeWatchers(IEnumerable<string> keys) => new((KeyWatchers ?? Enumerable.Empty<string>()).Concat(keys));

    /// <summary>
    /// Add the provided key to the watched fields of returned ParseObjects.
    /// If this is called multiple times, then all the keys specified in each of
    /// the calls will be watched.
    /// </summary>
    /// <param name="watch">The key that should be watched.</param>
    /// <returns>A new query with the additional constraint.</returns>
    public ParseLiveQuery<T> Watch(string watch) => new(this, new List<string> { watch });

    internal IDictionary<string, string> BuildParameters(bool includeClassName = false)
    {
        Dictionary<string, string> result = new Dictionary<string, string>();
        if (Filters != null)
            result["where"] = Filters;
        if (KeySelections != null)
            result["keys"] = String.Join(",", KeySelections.ToArray());
        if (KeyWatchers != null)
            result["watch"] = String.Join(",", KeyWatchers.ToArray());
        if (includeClassName)
            result["className"] = ClassName;
        return result;
    }

    /// <summary>
    /// Subscribes to the live query, allowing the client to receive real-time updates
    /// for the query's results. This establishes a subscription with the Live Query service.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous subscription operation. Upon completion
    /// of the task, the subscription is successfully registered.
    /// </returns>
    public async Task<IParseLiveQuerySubscription> SubscribeAsync()
    {
        return await Services.LiveQueryController.SubscribeAsync(this, CancellationToken.None);
    }
}