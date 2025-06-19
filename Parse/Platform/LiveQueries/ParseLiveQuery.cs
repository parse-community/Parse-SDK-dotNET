using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualBasic.CompilerServices;
using Parse.Abstractions.Infrastructure;
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
    Dictionary<string, object> Filters { get; }

    /// <summary>
    /// Serialized key selections.
    /// </summary>
    ReadOnlyCollection<string> KeySelections { get; }

    /// <summary>
    /// Serialized keys watched.
    /// </summary>
    ReadOnlyCollection<string> KeyWatch { get; }

    internal string ClassName { get; }

    internal IServiceHub Services { get; }

    private int RequestId = 0;

    public ParseLiveQuery(IServiceHub serviceHub, string className, IDictionary<string, object> filters, IEnumerable<string> selectedKeys = null, IEnumerable<string> watchedKeys = null)
    {
        if (filters.Count == 0)
        {
            // Throw error
        }

        Services = serviceHub;
        ClassName = className;

        Filters = new Dictionary<string, object>(filters);
        if (selectedKeys is not null)
        {
            KeySelections = new ReadOnlyCollection<string>(selectedKeys.ToList());
        }

        if (watchedKeys is not null)
        {
            KeyWatch = new ReadOnlyCollection<string>(watchedKeys.ToList());
        }
    }

    /// <summary>
    /// Private constructor for composition of queries. A source query is required,
    /// but the remaining values can be null if they aren't changed in this
    /// composition.
    /// </summary>
    internal ParseLiveQuery(ParseLiveQuery<T> source, IEnumerable<string> watchedKeys = null)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        Services = source.Services;
        ClassName = source.ClassName;
        Filters = source.Filters;
        KeySelections = source.KeySelections;

        if (watchedKeys is { })
        {
            KeyWatch = new ReadOnlyCollection<string>(MergeKeys(watchedKeys).ToList());
        }
    }

    HashSet<string> MergeKeys(IEnumerable<string> selectedKeys) => new((KeySelections ?? Enumerable.Empty<string>()).Concat(selectedKeys));

    /// <summary>
    /// Add the provided key to the watched fields of returned ParseObjects.
    /// If this is called multiple times, then all the keys specified in each of
    /// the calls will be watched.
    /// </summary>
    /// <param name="watch">The key that should be watched.</param>
    /// <returns>A new query with the additional constraint.</returns>
    public ParseLiveQuery<T> Watch(string watch) => new(this, new List<string> { watch });

    internal IDictionary<string, object> BuildParameters(bool includeClassName = false)
    {
        Dictionary<string, object> result = new Dictionary<string, object>();
        if (Filters != null)
            result["where"] = PointerOrLocalIdEncoder.Instance.Encode(Filters, Services);
        if (KeySelections != null)
            result["keys"] = String.Join(",", KeySelections.ToArray());
        if (KeyWatch != null)
            result["watch"] = String.Join(",", KeyWatch.ToArray());
        if (includeClassName)
            result["className"] = ClassName;
        return result;
    }

    /// <summary>
    /// Establishes a connection to the Parse Live Query server using the ClientWebSocket instance.
    /// Prepares and sends a connection message containing required identifiers such as application ID, client key, and session token.
    /// </summary>
    /// <returns>A Task representing the asynchronous operation, returning true if the connection attempt is initialized successfully, false otherwise.</returns>
    public async Task ConnectAsync()
    {
        await Services.LiveQueryController.ConnectAsync(CancellationToken.None);
    }

    /// <summary>
    /// Subscribes to the live query, allowing the client to receive real-time updates
    /// for the query's results. This establishes a subscription with the Live Query service.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous subscription operation. Upon completion
    /// of the task, the subscription is successfully registered.
    /// </returns>
    public async Task SubscribeAsync()
    {
        RequestId = await Services.LiveQueryController.SubscribeAsync(this, CancellationToken.None);
    }

    /// <summary>
    /// Unsubscribes from the live query, stopping the client from receiving further updates related to the subscription.
    /// </summary>
    /// <returns>A task representing the asynchronous operation of unsubscribing from the live query.</returns>
    public async Task UnsubscribeAsync()
    {
        if (RequestId > 0)
            await Services.LiveQueryController.UnsubscribeAsync(RequestId, CancellationToken.None);
    }

    /// <summary>
    /// Closes the connection to the live query server asynchronously.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation of closing the live query connection.
    /// </returns>
    public async Task CloseAsync()
    {
        await Services.LiveQueryController.CloseAsync(CancellationToken.None);
    }

}