using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.LiveQueries;

namespace Parse;

/// <summary>
/// The ParseLiveQuery class provides functionality to create and manage real-time queries on the Parse Server.
/// It allows tracking changes on objects of a specified class that match query constraints, such as filters
/// and watched fields, delivering updates in real-time as changes occur.
/// </summary>
/// <typeparam name="T">Represents the type of ParseObject that this query operates on. T must inherit from ParseObject.</typeparam>
public class ParseLiveQuery<T> where T : ParseObject
{
    /// <summary>
    /// Serialized <see langword="where"/> clauses.
    /// </summary>
    IDictionary<string, object> Filters { get; }

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

    public ParseLiveQuery(IServiceHub serviceHub, string className, IDictionary<string, object> filters, IEnumerable<string> selectedKeys = null, IEnumerable<string> watchedKeys = null)
    {
        ArgumentNullException.ThrowIfNull(filters);

        Services = serviceHub;
        ClassName = className;
        Filters = filters;

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
    private ParseLiveQuery(ParseLiveQuery<T> source, IEnumerable<string> watchedKeys = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        Services = source.Services;
        ClassName = source.ClassName;
        Filters = source.Filters;
        KeySelections = source.KeySelections;
        KeyWatchers = source.KeyWatchers;

        if (watchedKeys is not null)
        {
            KeyWatchers = new ReadOnlyCollection<string>(MergeWatchers(watchedKeys).ToList());
        }
    }

    private HashSet<string> MergeWatchers(IEnumerable<string> keys) => [..(KeyWatchers ?? Enumerable.Empty<string>()).Concat(keys)];

    /// <summary>
    /// Add the provided key to the watched fields of returned ParseObjects.
    /// If this is called multiple times, then all the keys specified in each of
    /// the calls will be watched.
    /// </summary>
    /// <param name="watch">The key that should be watched.</param>
    /// <returns>A new query with the additional constraint.</returns>
    public ParseLiveQuery<T> Watch(string watch) => new(this, new List<string> { watch });

    internal IDictionary<string, object> BuildParameters()
    {
        Dictionary<string, object> result = new Dictionary<string, object> { ["className"] = ClassName, ["where"] = Filters };
        if (KeySelections != null)
            result["keys"] = KeySelections.ToArray();
        if (KeyWatchers != null)
            result["watch"] = KeyWatchers.ToArray();
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
    public async Task<IParseLiveQuerySubscription> SubscribeAsync() =>
        await Services.LiveQueryController.SubscribeAsync(this, CancellationToken.None);
}