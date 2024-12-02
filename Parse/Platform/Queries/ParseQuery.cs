using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Abstractions.Platform.Objects;
using Parse.Infrastructure;
using Parse.Infrastructure.Data;
using Parse.Infrastructure.Utilities;

namespace Parse
{
    /// <summary>
    /// The ParseQuery class defines a query that is used to fetch ParseObjects. The
    /// most common use case is finding all objects that match a query through the
    /// <see cref="FindAsync()"/> method.
    /// </summary>
    /// <example>
    /// This sample code fetches all objects of
    /// class <c>"MyClass"</c>:
    ///
    /// <code>
    /// ParseQuery query = new ParseQuery("MyClass");
    /// IEnumerable&lt;ParseObject&gt; result = await query.FindAsync();
    /// </code>
    ///
    /// A ParseQuery can also be used to retrieve a single object whose id is known,
    /// through the <see cref="GetAsync(String)"/> method. For example, this sample code
    /// fetches an object of class <c>"MyClass"</c> and id <c>myId</c>.
    ///
    /// <code>
    /// ParseQuery query = new ParseQuery("MyClass");
    /// ParseObject result = await query.GetAsync(myId);
    /// </code>
    ///
    /// A ParseQuery can also be used to count the number of objects that match the
    /// query without retrieving all of those objects. For example, this sample code
    /// counts the number of objects of the class <c>"MyClass"</c>.
    ///
    /// <code>
    /// ParseQuery query = new ParseQuery("MyClass");
    /// int count = await query.CountAsync();
    /// </code>
    /// </example>
    public class ParseQuery<T> where T : ParseObject
    {
        /// <summary>
        /// Serialized <see langword="where"/> clauses.
        /// </summary>
        Dictionary<string, object> Filters { get; }

        /// <summary>
        /// Serialized <see langword="orderby"/> clauses.
        /// </summary>
        ReadOnlyCollection<string> Orderings { get; }

        /// <summary>
        /// Serialized related data query merging request (data inclusion) clauses.
        /// </summary>
        ReadOnlyCollection<string> Includes { get; }

        /// <summary>
        /// Serialized key selections.
        /// </summary>
        ReadOnlyCollection<string> KeySelections { get; }

        string RedirectClassNameForKey { get; }

        int? SkipAmount { get; }

        int? LimitAmount { get; }

        internal string ClassName { get; }

        internal IServiceHub Services { get; }

        /// <summary>
        /// Private constructor for composition of queries. A source query is required,
        /// but the remaining values can be null if they won't be changed in this
        /// composition.
        /// </summary>
        internal ParseQuery(ParseQuery<T> source, IDictionary<string, object> where = null, IEnumerable<string> replacementOrderBy = null, IEnumerable<string> thenBy = null, int? skip = null, int? limit = null, IEnumerable<string> includes = null, IEnumerable<string> selectedKeys = null, string redirectClassNameForKey = null)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Services = source.Services;
            ClassName = source.ClassName;
            Filters = source.Filters;
            Orderings = replacementOrderBy is null ? source.Orderings : new ReadOnlyCollection<string>(replacementOrderBy.ToList());

            // 0 could be handled differently from null.

            SkipAmount = skip is null ? source.SkipAmount : (source.SkipAmount ?? 0) + skip;
            LimitAmount = limit ?? source.LimitAmount;
            Includes = source.Includes;
            KeySelections = source.KeySelections;
            RedirectClassNameForKey = redirectClassNameForKey ?? source.RedirectClassNameForKey;

            if (thenBy is { })
            {
                List<string> newOrderBy = new List<string>(Orderings ?? throw new ArgumentException("You must call OrderBy before calling ThenBy."));

                newOrderBy.AddRange(thenBy);
                Orderings = new ReadOnlyCollection<string>(newOrderBy);
            }

            // Remove duplicates.

            if (Orderings is { })
            {
                Orderings = new ReadOnlyCollection<string>(new HashSet<string>(Orderings).ToList());
            }

            if (where is { })
            {
                Filters = new Dictionary<string, object>(MergeWhereClauses(where));
            }

            if (includes is { })
            {
                Includes = new ReadOnlyCollection<string>(MergeIncludes(includes).ToList());
            }

            if (selectedKeys is { })
            {
                KeySelections = new ReadOnlyCollection<string>(MergeSelectedKeys(selectedKeys).ToList());
            }
        }

        HashSet<string> MergeIncludes(IEnumerable<string> includes)
        {
            if (Includes is null)
            {
                return new HashSet<string>(includes);
            }

            HashSet<string> newIncludes = new HashSet<string>(Includes);

            foreach (string item in includes)
            {
                newIncludes.Add(item);
            }

            return newIncludes;
        }

        HashSet<string> MergeSelectedKeys(IEnumerable<string> selectedKeys)
        {
            return new HashSet<string>((KeySelections ?? Enumerable.Empty<string>()).Concat(selectedKeys));
        }

        IDictionary<string, object> MergeWhereClauses(IDictionary<string, object> where)
        {
            if (Filters is null)
            {
                return where;
            }

            Dictionary<string, object> newWhere = new Dictionary<string, object>(Filters);
            foreach (KeyValuePair<string, object> pair in where)
            {
                if (newWhere.ContainsKey(pair.Key))
                {
                    if (!(newWhere[pair.Key] is IDictionary<string, object> oldCondition) || !(pair.Value is IDictionary<string, object> condition))
                    {
                        throw new ArgumentException("More than one where clause for the given key provided.");
                    }

                    Dictionary<string, object> newCondition = new Dictionary<string, object>(oldCondition);
                    foreach (KeyValuePair<string, object> conditionPair in condition)
                    {
                        if (newCondition.ContainsKey(conditionPair.Key))
                        {
                            throw new ArgumentException("More than one condition for the given key provided.");
                        }

                        newCondition[conditionPair.Key] = conditionPair.Value;
                    }

                    newWhere[pair.Key] = newCondition;
                }
                else
                {
                    newWhere[pair.Key] = pair.Value;
                }
            }
            return newWhere;
        }

        /// <summary>
        /// Constructs a query based upon the ParseObject subclass used as the generic parameter for the ParseQuery.
        /// </summary>
        public ParseQuery(IServiceHub serviceHub) : this(serviceHub, serviceHub.ClassController.GetClassName(typeof(T))) { }

        /// <summary>
        /// Constructs a query. A default query with no further parameters will retrieve
        /// all <see cref="ParseObject"/>s of the provided class.
        /// </summary>
        /// <param name="className">The name of the class to retrieve ParseObjects for.</param>
        public ParseQuery(IServiceHub serviceHub, string className) => (ClassName, Services) = (className ?? throw new ArgumentNullException(nameof(className), "Must specify a ParseObject class name when creating a ParseQuery."), serviceHub);

        #region Order By

        /// <summary>
        /// Sorts the results in ascending order by the given key.
        /// This will override any existing ordering for the query.
        /// </summary>
        /// <param name="key">The key to order by.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> OrderBy(string key)
        {
            return new ParseQuery<T>(this, replacementOrderBy: new List<string> { key });
        }

        /// <summary>
        /// Sorts the results in descending order by the given key.
        /// This will override any existing ordering for the query.
        /// </summary>
        /// <param name="key">The key to order by.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> OrderByDescending(string key)
        {
            return new ParseQuery<T>(this, replacementOrderBy: new List<string> { "-" + key });
        }

        /// <summary>
        /// Sorts the results in ascending order by the given key, after previous
        /// ordering has been applied.
        ///
        /// This method can only be called if there is already an <see cref="OrderBy"/>
        /// or <see cref="OrderByDescending"/>
        /// on this query.
        /// </summary>
        /// <param name="key">The key to order by.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> ThenBy(string key)
        {
            return new ParseQuery<T>(this, thenBy: new List<string> { key });
        }

        /// <summary>
        /// Sorts the results in descending order by the given key, after previous
        /// ordering has been applied.
        ///
        /// This method can only be called if there is already an <see cref="OrderBy"/>
        /// or <see cref="OrderByDescending"/> on this query.
        /// </summary>
        /// <param name="key">The key to order by.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> ThenByDescending(string key)
        {
            return new ParseQuery<T>(this, thenBy: new List<string> { "-" + key });
        }

        #endregion

        /// <summary>
        /// Include nested ParseObjects for the provided key. You can use dot notation
        /// to specify which fields in the included objects should also be fetched.
        /// </summary>
        /// <param name="key">The key that should be included.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> Include(string key)
        {
            return new ParseQuery<T>(this, includes: new List<string> { key });
        }

        /// <summary>
        /// Restrict the fields of returned ParseObjects to only include the provided key.
        /// If this is called multiple times, then all of the keys specified in each of
        /// the calls will be included.
        /// </summary>
        /// <param name="key">The key that should be included.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> Select(string key)
        {
            return new ParseQuery<T>(this, selectedKeys: new List<string> { key });
        }

        /// <summary>
        /// Skips a number of results before returning. This is useful for pagination
        /// of large queries. Chaining multiple skips together will cause more results
        /// to be skipped.
        /// </summary>
        /// <param name="count">The number of results to skip.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> Skip(int count)
        {
            return new ParseQuery<T>(this, skip: count);
        }

        /// <summary>
        /// Controls the maximum number of results that are returned. Setting a negative
        /// limit denotes retrieval without a limit. Chaining multiple limits
        /// results in the last limit specified being used. The default limit is
        /// 100, with a maximum of 1000 results being returned at a time.
        /// </summary>
        /// <param name="count">The maximum number of results to return.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> Limit(int count)
        {
            return new ParseQuery<T>(this, limit: count);
        }

        internal ParseQuery<T> RedirectClassName(string key)
        {
            return new ParseQuery<T>(this, redirectClassNameForKey: key);
        }

        #region Where

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value to be
        /// contained in the provided list of values.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="values">The values that will match.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> WhereContainedIn<TIn>(string key, IEnumerable<TIn> values)
        {
            return new ParseQuery<T>(this, where: new Dictionary<string, object> { { key, new Dictionary<string, object> { { "$in", values.ToList() } } } });
        }

        /// <summary>
        /// Add a constraint to the querey that requires a particular key's value to be
        /// a list containing all of the elements in the provided list of values.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="values">The values that will match.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> WhereContainsAll<TIn>(string key, IEnumerable<TIn> values)
        {
            return new ParseQuery<T>(this, where: new Dictionary<string, object> { { key, new Dictionary<string, object> { { "$all", values.ToList() } } } });
        }

        /// <summary>
        /// Adds a constraint for finding string values that contain a provided string.
        /// This will be slow for large data sets.
        /// </summary>
        /// <param name="key">The key that the string to match is stored in.</param>
        /// <param name="substring">The substring that the value must contain.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> WhereContains(string key, string substring)
        {
            return new ParseQuery<T>(this, where: new Dictionary<string, object> { { key, new Dictionary<string, object> { { "$regex", RegexQuote(substring) } } } });
        }

        /// <summary>
        /// Adds a constraint for finding objects that do not contain a given key.
        /// </summary>
        /// <param name="key">The key that should not exist.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> WhereDoesNotExist(string key)
        {
            return new ParseQuery<T>(this, where: new Dictionary<string, object> { { key, new Dictionary<string, object> { { "$exists", false } } } });
        }

        /// <summary>
        /// Adds a constraint to the query that requires that a particular key's value
        /// does not match another ParseQuery. This only works on keys whose values are
        /// ParseObjects or lists of ParseObjects.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="query">The query that the value should not match.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> WhereDoesNotMatchQuery<TOther>(string key, ParseQuery<TOther> query) where TOther : ParseObject
        {
            return new ParseQuery<T>(this, where: new Dictionary<string, object> { { key, new Dictionary<string, object> { { "$notInQuery", query.BuildParameters(true) } } } });
        }

        /// <summary>
        /// Adds a constraint for finding string values that end with a provided string.
        /// This will be slow for large data sets.
        /// </summary>
        /// <param name="key">The key that the string to match is stored in.</param>
        /// <param name="suffix">The substring that the value must end with.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> WhereEndsWith(string key, string suffix)
        {
            return new ParseQuery<T>(this, where: new Dictionary<string, object> { { key, new Dictionary<string, object> { { "$regex", RegexQuote(suffix) + "$" } } } });
        }

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value to be
        /// equal to the provided value.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="value">The value that the ParseObject must contain.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> WhereEqualTo(string key, object value)
        {
            return new ParseQuery<T>(this, where: new Dictionary<string, object> { { key, value } });
        }

        /// <summary>
        /// Adds a constraint for finding objects that contain a given key.
        /// </summary>
        /// <param name="key">The key that should exist.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> WhereExists(string key)
        {
            return new ParseQuery<T>(this, where: new Dictionary<string, object> { { key, new Dictionary<string, object> { { "$exists", true } } } });
        }

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value to be
        /// greater than the provided value.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="value">The value that provides a lower bound.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> WhereGreaterThan(string key, object value)
        {
            return new ParseQuery<T>(this, where: new Dictionary<string, object> { { key, new Dictionary<string, object> { { "$gt", value } } } });
        }

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value to be
        /// greater or equal to than the provided value.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="value">The value that provides a lower bound.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> WhereGreaterThanOrEqualTo(string key, object value)
        {
            return new ParseQuery<T>(this, where: new Dictionary<string, object> { { key, new Dictionary<string, object> { { "$gte", value } } } });
        }

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value to be
        /// less than the provided value.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="value">The value that provides an upper bound.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> WhereLessThan(string key, object value)
        {
            return new ParseQuery<T>(this, where: new Dictionary<string, object> { { key, new Dictionary<string, object> { { "$lt", value } } } });
        }

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value to be
        /// less than or equal to the provided value.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="value">The value that provides a lower bound.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> WhereLessThanOrEqualTo(string key, object value)
        {
            return new ParseQuery<T>(this, where: new Dictionary<string, object> { { key, new Dictionary<string, object> { { "$lte", value } } } });
        }

        /// <summary>
        /// Adds a regular expression constraint for finding string values that match the provided
        /// regular expression. This may be slow for large data sets.
        /// </summary>
        /// <param name="key">The key that the string to match is stored in.</param>
        /// <param name="regex">The regular expression pattern to match. The Regex must
        /// have the <see cref="RegexOptions.ECMAScript"/> options flag set.</param>
        /// <param name="modifiers">Any of the following supported PCRE modifiers:
        /// <code>i</code> - Case insensitive search
        /// <code>m</code> Search across multiple lines of input</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> WhereMatches(string key, Regex regex, string modifiers)
        {
            return !regex.Options.HasFlag(RegexOptions.ECMAScript) ? throw new ArgumentException("Only ECMAScript-compatible regexes are supported. Please use the ECMAScript RegexOptions flag when creating your regex.") : new ParseQuery<T>(this, where: new Dictionary<string, object> { { key, EncodeRegex(regex, modifiers) } });
        }

        /// <summary>
        /// Adds a regular expression constraint for finding string values that match the provided
        /// regular expression. This may be slow for large data sets.
        /// </summary>
        /// <param name="key">The key that the string to match is stored in.</param>
        /// <param name="regex">The regular expression pattern to match. The Regex must
        /// have the <see cref="RegexOptions.ECMAScript"/> options flag set.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> WhereMatches(string key, Regex regex)
        {
            return WhereMatches(key, regex, null);
        }

        /// <summary>
        /// Adds a regular expression constraint for finding string values that match the provided
        /// regular expression. This may be slow for large data sets.
        /// </summary>
        /// <param name="key">The key that the string to match is stored in.</param>
        /// <param name="pattern">The PCRE regular expression pattern to match.</param>
        /// <param name="modifiers">Any of the following supported PCRE modifiers:
        /// <code>i</code> - Case insensitive search
        /// <code>m</code> Search across multiple lines of input</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> WhereMatches(string key, string pattern, string modifiers = null)
        {
            return WhereMatches(key, new Regex(pattern, RegexOptions.ECMAScript), modifiers);
        }

        /// <summary>
        /// Adds a regular expression constraint for finding string values that match the provided
        /// regular expression. This may be slow for large data sets.
        /// </summary>
        /// <param name="key">The key that the string to match is stored in.</param>
        /// <param name="pattern">The PCRE regular expression pattern to match.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> WhereMatches(string key, string pattern)
        {
            return WhereMatches(key, pattern, null);
        }

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value
        /// to match a value for a key in the results of another ParseQuery.
        /// </summary>
        /// <param name="key">The key whose value is being checked.</param>
        /// <param name="keyInQuery">The key in the objects from the subquery to look in.</param>
        /// <param name="query">The subquery to run</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> WhereMatchesKeyInQuery<TOther>(string key, string keyInQuery, ParseQuery<TOther> query) where TOther : ParseObject
        {
            return new ParseQuery<T>(this, where: new Dictionary<string, object>
            {
                [key] = new Dictionary<string, object>
                {
                    ["$select"] = new Dictionary<string, object>
                    {
                        [nameof(query)] = query.BuildParameters(true),
                        [nameof(key)] = keyInQuery
                    }
                }
            });
        }

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value
        /// does not match any value for a key in the results of another ParseQuery.
        /// </summary>
        /// <param name="key">The key whose value is being checked.</param>
        /// <param name="keyInQuery">The key in the objects from the subquery to look in.</param>
        /// <param name="query">The subquery to run</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> WhereDoesNotMatchesKeyInQuery<TOther>(string key, string keyInQuery, ParseQuery<TOther> query) where TOther : ParseObject
        {
            return new ParseQuery<T>(this, where: new Dictionary<string, object>
            {
                [key] = new Dictionary<string, object>
                {
                    ["$dontSelect"] = new Dictionary<string, object>
                    {
                        [nameof(query)] = query.BuildParameters(true),
                        [nameof(key)] = keyInQuery
                    }
                }
            });
        }

        /// <summary>
        /// Adds a constraint to the query that requires that a particular key's value
        /// matches another ParseQuery. This only works on keys whose values are
        /// ParseObjects or lists of ParseObjects.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="query">The query that the value should match.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> WhereMatchesQuery<TOther>(string key, ParseQuery<TOther> query) where TOther : ParseObject
        {
            return new ParseQuery<T>(this, where: new Dictionary<string, object>
            {
                [key] = new Dictionary<string, object>
                {
                    ["$inQuery"] = query.BuildParameters(true)
                }
            });
        }

        /// <summary>
        /// Adds a proximity-based constraint for finding objects with keys whose GeoPoint
        /// values are near the given point.
        /// </summary>
        /// <param name="key">The key that the ParseGeoPoint is stored in.</param>
        /// <param name="point">The reference ParseGeoPoint.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> WhereNear(string key, ParseGeoPoint point)
        {
            return new ParseQuery<T>(this, where: new Dictionary<string, object>
            {
                [key] = new Dictionary<string, object>
                {
                    ["$nearSphere"] = point
                }
            });
        }

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value to be
        /// contained in the provided list of values.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="values">The values that will match.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> WhereNotContainedIn<TIn>(string key, IEnumerable<TIn> values)
        {
            return new ParseQuery<T>(this, where: new Dictionary<string, object>
            {
                [key] = new Dictionary<string, object>
                {
                    ["$nin"] = values.ToList()
                }
            });
        }

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value not
        /// to be equal to the provided value.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="value">The value that that must not be equalled.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> WhereNotEqualTo(string key, object value)
        {
            return new ParseQuery<T>(this, where: new Dictionary<string, object>
            {
                [key] = new Dictionary<string, object>
                {
                    ["$ne"] = value
                }
            });
        }

        /// <summary>
        /// Adds a constraint for finding string values that start with the provided string.
        /// This query will use the backend index, so it will be fast even with large data sets.
        /// </summary>
        /// <param name="key">The key that the string to match is stored in.</param>
        /// <param name="suffix">The substring that the value must start with.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> WhereStartsWith(string key, string suffix)
        {
            return new ParseQuery<T>(this, where: new Dictionary<string, object>
            {
                [key] = new Dictionary<string, object>
                {
                    ["$regex"] = $"^{RegexQuote(suffix)}"
                }
            });
        }

        /// <summary>
        /// Add a constraint to the query that requires a particular key's coordinates to be
        /// contained within a given rectangular geographic bounding box.
        /// </summary>
        /// <param name="key">The key to be constrained.</param>
        /// <param name="southwest">The lower-left inclusive corner of the box.</param>
        /// <param name="northeast">The upper-right inclusive corner of the box.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> WhereWithinGeoBox(string key, ParseGeoPoint southwest, ParseGeoPoint northeast)
        {
            return new ParseQuery<T>(this, where: new Dictionary<string, object>
            {
                [key] = new Dictionary<string, object>
                {
                    ["$within"] = new Dictionary<string, object>
                    {
                        ["$box"] = new[]
                    {
                        southwest,
                        northeast
                    }
                    }
                }
            });
        }

        /// <summary>
        /// Adds a proximity-based constraint for finding objects with keys whose GeoPoint
        /// values are near the given point and within the maximum distance given.
        /// </summary>
        /// <param name="key">The key that the ParseGeoPoint is stored in.</param>
        /// <param name="point">The reference ParseGeoPoint.</param>
        /// <param name="maxDistance">The maximum distance (in radians) of results to return.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public ParseQuery<T> WhereWithinDistance(string key, ParseGeoPoint point, ParseGeoDistance maxDistance)
        {
            return new ParseQuery<T>(WhereNear(key, point), where: new Dictionary<string, object>
            {
                [key] = new Dictionary<string, object>
                {
                    ["$maxDistance"] = maxDistance.Radians
                }
            });
        }

        internal ParseQuery<T> WhereRelatedTo(ParseObject parent, string key)
        {
            return new ParseQuery<T>(this, where: new Dictionary<string, object>
            {
                ["$relatedTo"] = new Dictionary<string, object>
                {
                    ["object"] = parent,
                    [nameof(key)] = key
                }
            });
        }

        #endregion

        /// <summary>
        /// Retrieves a list of ParseObjects that satisfy this query from Parse.
        /// </summary>
        /// <returns>The list of ParseObjects that match this query.</returns>
        public Task<IEnumerable<T>> FindAsync()
        {
            return FindAsync(CancellationToken.None);
        }
        /// <summary>
        /// Retrieves a list of ParseObjects that satisfy this query from Parse.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The list of ParseObjects that match this query.</returns>
        public async Task<IEnumerable<T>> FindAsync(CancellationToken cancellationToken)
        {
            EnsureNotInstallationQuery();
            var result = await Services.QueryController.FindAsync(this, Services.GetCurrentUser(), cancellationToken).ConfigureAwait(false);
            return result.Select(state => Services.GenerateObjectFromState<T>(state, ClassName));
        }

        /// <summary>
        /// Retrieves at most one ParseObject that satisfies this query.
        /// </summary>
        /// <returns>A single ParseObject that satisfies this query, or else null.</returns>
        public Task<T> FirstOrDefaultAsync()
        {
            return FirstOrDefaultAsync(CancellationToken.None);
        }

        /// <summary>
        /// Retrieves at most one ParseObject that satisfies this query.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A single ParseObject that satisfies this query, or else null.</returns>
        public async Task<T> FirstOrDefaultAsync(CancellationToken cancellationToken)
        {
            EnsureNotInstallationQuery();
            var result = await Services.QueryController.FirstAsync(this, Services.GetCurrentUser(), cancellationToken).ConfigureAwait(false);

            return result != null
                ? Services.GenerateObjectFromState<T>(result, ClassName)
                : default; // Return default value (null for reference types) if result is null
        }


        /// <summary>
        /// Retrieves at most one ParseObject that satisfies this query.
        /// </summary>
        /// <returns>A single ParseObject that satisfies this query.</returns>
        /// <exception cref="ParseFailureException">If no results match the query.</exception>
        public Task<T> FirstAsync()
        {
            return FirstAsync(CancellationToken.None);
        }

        /// <summary>
        /// Retrieves at most one ParseObject that satisfies this query.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A single ParseObject that satisfies this query.</returns>
        /// <exception cref="ParseFailureException">If no results match the query.</exception>
        public async Task<T> FirstAsync(CancellationToken cancellationToken)
        {
            var result = await FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (result == null)
            {
                throw new ParseFailureException(ParseFailureException.ErrorCode.ObjectNotFound, "No results matched the query.");
            }
            return result;
        }

        /// <summary>
        /// Counts the number of objects that match this query.
        /// </summary>
        /// <returns>The number of objects that match this query.</returns>
        public Task<int> CountAsync()
        {
            return CountAsync(CancellationToken.None);
        }

        /// <summary>
        /// Counts the number of objects that match this query.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The number of objects that match this query.</returns>
        public Task<int> CountAsync(CancellationToken cancellationToken)
        {
            EnsureNotInstallationQuery();
            return Services.QueryController.CountAsync(this, Services.GetCurrentUser(), cancellationToken);
        }

        /// <summary>
        /// Constructs a ParseObject whose id is already known by fetching data
        /// from the server.
        /// </summary>
        /// <param name="objectId">ObjectId of the ParseObject to fetch.</param>
        /// <returns>The ParseObject for the given objectId.</returns>
        public Task<T> GetAsync(string objectId)
        {
            return GetAsync(objectId, CancellationToken.None);
        }

        /// <summary>
        /// Constructs a ParseObject whose id is already known by fetching data
        /// from the server.
        /// </summary>
        /// <param name="objectId">ObjectId of the ParseObject to fetch.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The ParseObject for the given objectId.</returns>
        public async Task<T> GetAsync(string objectId, CancellationToken cancellationToken)
        {
            var query = new ParseQuery<T>(Services, ClassName)
                .WhereEqualTo(nameof(objectId), objectId)
                .Limit(1);

            var result = await query.FindAsync(cancellationToken).ConfigureAwait(false);
            return result.FirstOrDefault() ?? throw new ParseFailureException(ParseFailureException.ErrorCode.ObjectNotFound, "Object with the given objectId not found.");
        }

        internal object GetConstraint(string key)
        {
            return Filters?.GetOrDefault(key, null);
        }

        internal IDictionary<string, object> BuildParameters(bool includeClassName = false)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (Filters != null)
                result["where"] = PointerOrLocalIdEncoder.Instance.Encode(Filters, Services);
            if (Orderings != null)
                result["order"] = String.Join(",", Orderings.ToArray());
            if (SkipAmount != null)
                result["skip"] = SkipAmount.Value;
            if (LimitAmount != null)
                result["limit"] = LimitAmount.Value;
            if (Includes != null)
                result["include"] = String.Join(",", Includes.ToArray());
            if (KeySelections != null)
                result["keys"] = String.Join(",", KeySelections.ToArray());
            if (includeClassName)
                result["className"] = ClassName;
            if (RedirectClassNameForKey != null)
                result["redirectClassNameForKey"] = RedirectClassNameForKey;
            return result;
        }

        string RegexQuote(string input)
        {
            return "\\Q" + input.Replace("\\E", "\\E\\\\E\\Q") + "\\E";
        }

        string GetRegexOptions(Regex regex, string modifiers)
        {
            string result = modifiers ?? "";
            if (regex.Options.HasFlag(RegexOptions.IgnoreCase) && !modifiers.Contains("i"))
                result += "i";
            if (regex.Options.HasFlag(RegexOptions.Multiline) && !modifiers.Contains("m"))
                result += "m";
            return result;
        }

        IDictionary<string, object> EncodeRegex(Regex regex, string modifiers)
        {
            string options = GetRegexOptions(regex, modifiers);
            Dictionary<string, object> dict = new Dictionary<string, object> { ["$regex"] = regex.ToString() };

            if (!String.IsNullOrEmpty(options))
            {
                dict["$options"] = options;
            }

            return dict;
        }

        void EnsureNotInstallationQuery()
        {
            // The ParseInstallation class is not accessible from this project; using string literal.

            if (ClassName.Equals("_Installation"))
            {
                throw new InvalidOperationException("Cannot directly query the Installation class.");
            }
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c></returns>
        public override bool Equals(object obj)
        {
            return obj == null || !(obj is ParseQuery<T> other) ? false : Equals(ClassName, other.ClassName) && Filters.CollectionsEqual(other.Filters) && Orderings.CollectionsEqual(other.Orderings) && Includes.CollectionsEqual(other.Includes) && KeySelections.CollectionsEqual(other.KeySelections) && Equals(SkipAmount, other.SkipAmount) && Equals(LimitAmount, other.LimitAmount);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            // TODO (richardross): Implement this.
            return 0;
        }
    }
}
