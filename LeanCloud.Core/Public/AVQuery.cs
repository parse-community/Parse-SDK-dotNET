// Copyright (c) 2015-present, LeanCloud, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LeanCloud.Core.Internal;
using LeanCloud.Storage.Internal;

namespace LeanCloud
{

    /// <summary>
    /// The AVQuery class defines a query that is used to fetch ParseObjects. The
    /// most common use case is finding all objects that match a query through the
    /// <see cref="FindAsync()"/> method.
    /// </summary>
    /// <example>
    /// This sample code fetches all objects of
    /// class <c>"MyClass"</c>:
    ///
    /// <code>
    /// AVQuery query = new AVQuery("MyClass");
    /// IEnumerable&lt;AVObject&gt; result = await query.FindAsync();
    /// </code>
    ///
    /// A AVQuery can also be used to retrieve a single object whose id is known,
    /// through the <see cref="GetAsync(string)"/> method. For example, this sample code
    /// fetches an object of class <c>"MyClass"</c> and id <c>myId</c>.
    ///
    /// <code>
    /// AVQuery query = new AVQuery("MyClass");
    /// AVObject result = await query.GetAsync(myId);
    /// </code>
    ///
    /// A AVQuery can also be used to count the number of objects that match the
    /// query without retrieving all of those objects. For example, this sample code
    /// counts the number of objects of the class <c>"MyClass"</c>.
    ///
    /// <code>
    /// AVQuery query = new AVQuery("MyClass");
    /// int count = await query.CountAsync();
    /// </code>
    /// </example>
    public class AVQuery<T> where T : AVObject
    {
        private readonly string className;
        private readonly Dictionary<string, object> where;
        private readonly ReadOnlyCollection<string> orderBy;
        private readonly ReadOnlyCollection<string> includes;
        private readonly ReadOnlyCollection<string> selectedKeys;
        private readonly String redirectClassNameForKey;
        private readonly int? skip;
        private readonly int? limit;

        internal string ClassName { get { return className; } }

        internal static IAVQueryController QueryController
        {
            get
            {
                return AVPlugins.Instance.QueryController;
            }
        }

        internal static IObjectSubclassingController SubclassingController
        {
            get
            {
                return AVPlugins.Instance.SubclassingController;
            }
        }

        /// <summary>
        /// Private constructor for composition of queries. A source query is required,
        /// but the remaining values can be null if they won't be changed in this
        /// composition.
        /// </summary>
        private AVQuery(AVQuery<T> source,
            IDictionary<string, object> where = null,
            IEnumerable<string> replacementOrderBy = null,
            IEnumerable<string> thenBy = null,
            int? skip = null,
            int? limit = null,
            IEnumerable<string> includes = null,
            IEnumerable<string> selectedKeys = null,
            String redirectClassNameForKey = null)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            className = source.className;
            this.where = source.where;
            this.orderBy = source.orderBy;
            this.skip = source.skip;
            this.limit = source.limit;
            this.includes = source.includes;
            this.selectedKeys = source.selectedKeys;
            this.redirectClassNameForKey = source.redirectClassNameForKey;

            if (where != null)
            {
                var newWhere = MergeWhereClauses(where);
                this.where = new Dictionary<string, object>(newWhere);
            }

            if (replacementOrderBy != null)
            {
                this.orderBy = new ReadOnlyCollection<string>(replacementOrderBy.ToList());
            }

            if (thenBy != null)
            {
                if (this.orderBy == null)
                {
                    throw new ArgumentException("You must call OrderBy before calling ThenBy.");
                }
                var newOrderBy = new List<string>(this.orderBy);
                newOrderBy.AddRange(thenBy);
                this.orderBy = new ReadOnlyCollection<string>(newOrderBy);
            }

            // Remove duplicates.
            if (this.orderBy != null)
            {
                var newOrderBy = new HashSet<string>(this.orderBy);
                this.orderBy = new ReadOnlyCollection<string>(newOrderBy.ToList<string>());
            }

            if (skip != null)
            {
                this.skip = (this.skip ?? 0) + skip;
            }

            if (limit != null)
            {
                this.limit = limit;
            }

            if (includes != null)
            {
                var newIncludes = MergeIncludes(includes);
                this.includes = new ReadOnlyCollection<string>(newIncludes.ToList());
            }

            if (selectedKeys != null)
            {
                var newSelectedKeys = MergeSelectedKeys(selectedKeys);
                this.selectedKeys = new ReadOnlyCollection<string>(newSelectedKeys.ToList());
            }

            if (redirectClassNameForKey != null)
            {
                this.redirectClassNameForKey = redirectClassNameForKey;
            }
        }

        private HashSet<string> MergeIncludes(IEnumerable<string> includes)
        {
            if (this.includes == null)
            {
                return new HashSet<string>(includes);
            }
            var newIncludes = new HashSet<string>(this.includes);
            foreach (var item in includes)
            {
                newIncludes.Add(item);
            }
            return newIncludes;
        }

        private HashSet<String> MergeSelectedKeys(IEnumerable<String> selectedKeys)
        {
            if (this.selectedKeys == null)
            {
                return new HashSet<string>(selectedKeys);
            }
            var newSelectedKeys = new HashSet<String>(this.selectedKeys);
            foreach (var item in selectedKeys)
            {
                newSelectedKeys.Add(item);
            }
            return newSelectedKeys;
        }

        private IDictionary<string, object> MergeWhereClauses(IDictionary<string, object> where)
        {
            if (this.where == null)
            {
                return where;
            }
            var newWhere = new Dictionary<string, object>(this.where);
            foreach (var pair in where)
            {
                var condition = pair.Value as IDictionary<string, object>;
                if (newWhere.ContainsKey(pair.Key))
                {
                    var oldCondition = newWhere[pair.Key] as IDictionary<string, object>;
                    if (oldCondition == null || condition == null)
                    {
                        throw new ArgumentException("More than one where clause for the given key provided.");
                    }
                    var newCondition = new Dictionary<string, object>(oldCondition);
                    foreach (var conditionPair in condition)
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
        /// Constructs a query based upon the AVObject subclass used as the generic parameter for the AVQuery.
        /// </summary>
        public AVQuery()
          : this(SubclassingController.GetClassName(typeof(T)))
        {
        }

        /// <summary>
        /// Constructs a query. A default query with no further parameters will retrieve
        /// all <see cref="ParseObject"/>s of the provided class.
        /// </summary>
        /// <param name="className">The name of the class to retrieve ParseObjects for.</param>
        public AVQuery(string className)
        {
            if (className == null)
            {
                throw new ArgumentNullException("className", "Must specify a AVObject class name when creating a AVQuery.");
            }
            this.className = className;
        }

        /// <summary>
        /// Constructs a query that is the or of the given queries.
        /// </summary>
        /// <param name="queries">The list of ParseQueries to 'or' together.</param>
        /// <returns>A ParseQquery that is the 'or' of the passed in queries.</returns>
        public static AVQuery<T> Or(IEnumerable<AVQuery<T>> queries)
        {
            string className = null;
            var orValue = new List<IDictionary<string, object>>();
            // We need to cast it to non-generic IEnumerable because of AOT-limitation
            var nonGenericQueries = (IEnumerable)queries;
            foreach (var obj in nonGenericQueries)
            {
                var q = (AVQuery<T>)obj;
                if (className != null && q.className != className)
                {
                    throw new ArgumentException(
                        "All of the queries in an or query must be on the same class.");
                }
                className = q.className;
                var parameters = q.BuildParameters();
                if (parameters.Count == 0)
                {
                    continue;
                }
                object where;
                if (!parameters.TryGetValue("where", out where) || parameters.Count > 1)
                {
                    throw new ArgumentException(
                        "None of the queries in an or query can have non-filtering clauses");
                }
                orValue.Add(where as IDictionary<string, object>);
            }
            return new AVQuery<T>(new AVQuery<T>(className),
              where: new Dictionary<string, object> {
          {"$or", orValue}
              });
        }

        #region Order By

        /// <summary>
        /// Sorts the results in ascending order by the given key.
        /// This will override any existing ordering for the query.
        /// </summary>
        /// <param name="key">The key to order by.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public AVQuery<T> OrderBy(string key)
        {
            return new AVQuery<T>(this, replacementOrderBy: new List<string> { key });
        }

        /// <summary>
        /// Sorts the results in descending order by the given key.
        /// This will override any existing ordering for the query.
        /// </summary>
        /// <param name="key">The key to order by.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public AVQuery<T> OrderByDescending(string key)
        {
            return new AVQuery<T>(this, replacementOrderBy: new List<string> { "-" + key });
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
        public AVQuery<T> ThenBy(string key)
        {
            return new AVQuery<T>(this, thenBy: new List<string> { key });
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
        public AVQuery<T> ThenByDescending(string key)
        {
            return new AVQuery<T>(this, thenBy: new List<string> { "-" + key });
        }

        #endregion

        /// <summary>
        /// Include nested ParseObjects for the provided key. You can use dot notation
        /// to specify which fields in the included objects should also be fetched.
        /// </summary>
        /// <param name="key">The key that should be included.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public AVQuery<T> Include(string key)
        {
            return new AVQuery<T>(this, includes: new List<string> { key });
        }

        /// <summary>
        /// Restrict the fields of returned ParseObjects to only include the provided key.
        /// If this is called multiple times, then all of the keys specified in each of
        /// the calls will be included.
        /// </summary>
        /// <param name="key">The key that should be included.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public AVQuery<T> Select(string key)
        {
            return new AVQuery<T>(this, selectedKeys: new List<string> { key });
        }

        /// <summary>
        /// Skips a number of results before returning. This is useful for pagination
        /// of large queries. Chaining multiple skips together will cause more results
        /// to be skipped.
        /// </summary>
        /// <param name="count">The number of results to skip.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public AVQuery<T> Skip(int count)
        {
            return new AVQuery<T>(this, skip: count);
        }

        /// <summary>
        /// Controls the maximum number of results that are returned. Setting a negative
        /// limit denotes retrieval without a limit. Chaining multiple limits
        /// results in the last limit specified being used. The default limit is
        /// 100, with a maximum of 1000 results being returned at a time.
        /// </summary>
        /// <param name="count">The maximum number of results to return.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public AVQuery<T> Limit(int count)
        {
            return new AVQuery<T>(this, limit: count);
        }

        internal AVQuery<T> RedirectClassName(String key)
        {
            return new AVQuery<T>(this, redirectClassNameForKey: key);
        }

        #region Where

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value to be
        /// contained in the provided list of values.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="values">The values that will match.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public AVQuery<T> WhereContainedIn<TIn>(string key, IEnumerable<TIn> values)
        {
            return new AVQuery<T>(this, where: new Dictionary<string, object> {
        {key, new Dictionary<string, object>{{"$in", values.ToList()}}}
      });
        }

        /// <summary>
        /// Add a constraint to the querey that requires a particular key's value to be
        /// a list containing all of the elements in the provided list of values.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="values">The values that will match.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public AVQuery<T> WhereContainsAll<TIn>(string key, IEnumerable<TIn> values)
        {
            return new AVQuery<T>(this, where: new Dictionary<string, object> {
        {key, new Dictionary<string, object>{{"$all", values.ToList()}}}
      });
        }

        /// <summary>
        /// Adds a constraint for finding string values that contain a provided string.
        /// This will be slow for large data sets.
        /// </summary>
        /// <param name="key">The key that the string to match is stored in.</param>
        /// <param name="substring">The substring that the value must contain.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public AVQuery<T> WhereContains(string key, string substring)
        {
            return new AVQuery<T>(this, where: new Dictionary<string, object> {
        {key, new Dictionary<string, object>{{"$regex", RegexQuote(substring)}}}
      });
        }

        /// <summary>
        /// Adds a constraint for finding objects that do not contain a given key.
        /// </summary>
        /// <param name="key">The key that should not exist.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public AVQuery<T> WhereDoesNotExist(string key)
        {
            return new AVQuery<T>(this, where: new Dictionary<string, object>{
        {key, new Dictionary<string, object>{{"$exists", false}}}
      });
        }

        /// <summary>
        /// Adds a constraint to the query that requires that a particular key's value
        /// does not match another AVQuery. This only works on keys whose values are
        /// ParseObjects or lists of ParseObjects.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="query">The query that the value should not match.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public AVQuery<T> WhereDoesNotMatchQuery<TOther>(string key, AVQuery<TOther> query)
          where TOther : AVObject
        {
            return new AVQuery<T>(this, where: new Dictionary<string, object> {
        {key, new Dictionary<string, object>{{"$notInQuery", query.BuildParameters(true)}}}
      });
        }

        /// <summary>
        /// Adds a constraint for finding string values that end with a provided string.
        /// This will be slow for large data sets.
        /// </summary>
        /// <param name="key">The key that the string to match is stored in.</param>
        /// <param name="suffix">The substring that the value must end with.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public AVQuery<T> WhereEndsWith(string key, string suffix)
        {
            return new AVQuery<T>(this, where: new Dictionary<string, object> {
        {key, new Dictionary<string, object>{{"$regex", RegexQuote(suffix) + "$"}}}
      });
        }

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value to be
        /// equal to the provided value.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="value">The value that the AVObject must contain.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public AVQuery<T> WhereEqualTo(string key, object value)
        {
            return new AVQuery<T>(this, where: new Dictionary<string, object> {
        {key, value}
      });
        }

        /// <summary>
        /// Adds a constraint for finding objects that contain a given key.
        /// </summary>
        /// <param name="key">The key that should exist.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public AVQuery<T> WhereExists(string key)
        {
            return new AVQuery<T>(this, where: new Dictionary<string, object>{
        {key, new Dictionary<string, object>{{"$exists", true}}}
      });
        }

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value to be
        /// greater than the provided value.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="value">The value that provides a lower bound.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public AVQuery<T> WhereGreaterThan(string key, object value)
        {
            return new AVQuery<T>(this, where: new Dictionary<string, object>{
        {key, new Dictionary<string, object>{{"$gt", value}}}
      });
        }

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value to be
        /// greater or equal to than the provided value.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="value">The value that provides a lower bound.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public AVQuery<T> WhereGreaterThanOrEqualTo(string key, object value)
        {
            return new AVQuery<T>(this, where: new Dictionary<string, object>{
        {key, new Dictionary<string, object>{{"$gte", value}}}
      });
        }

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value to be
        /// less than the provided value.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="value">The value that provides an upper bound.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public AVQuery<T> WhereLessThan(string key, object value)
        {
            return new AVQuery<T>(this, where: new Dictionary<string, object>{
        {key, new Dictionary<string, object>{{"$lt", value}}}
      });
        }

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value to be
        /// less than or equal to the provided value.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="value">The value that provides a lower bound.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public AVQuery<T> WhereLessThanOrEqualTo(string key, object value)
        {
            return new AVQuery<T>(this, where: new Dictionary<string, object>{
        {key, new Dictionary<string, object>{{"$lte", value}}}
      });
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
        public AVQuery<T> WhereMatches(string key, Regex regex, string modifiers)
        {
            if (!regex.Options.HasFlag(RegexOptions.ECMAScript))
            {
                throw new ArgumentException(
                  "Only ECMAScript-compatible regexes are supported. Please use the ECMAScript RegexOptions flag when creating your regex.");
            }
            return new AVQuery<T>(this, where: new Dictionary<string, object> {
        {key, EncodeRegex(regex, modifiers)}
      });
        }

        /// <summary>
        /// Adds a regular expression constraint for finding string values that match the provided
        /// regular expression. This may be slow for large data sets.
        /// </summary>
        /// <param name="key">The key that the string to match is stored in.</param>
        /// <param name="regex">The regular expression pattern to match. The Regex must
        /// have the <see cref="RegexOptions.ECMAScript"/> options flag set.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public AVQuery<T> WhereMatches(string key, Regex regex)
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
        public AVQuery<T> WhereMatches(string key, string pattern, string modifiers = null)
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
        public AVQuery<T> WhereMatches(string key, string pattern)
        {
            return WhereMatches(key, pattern, null);
        }

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value
        /// to match a value for a key in the results of another AVQuery.
        /// </summary>
        /// <param name="key">The key whose value is being checked.</param>
        /// <param name="keyInQuery">The key in the objects from the subquery to look in.</param>
        /// <param name="query">The subquery to run</param>
        /// <returns>A new query with the additional constraint.</returns>
        public AVQuery<T> WhereMatchesKeyInQuery<TOther>(string key,
          string keyInQuery,
          AVQuery<TOther> query) where TOther : AVObject
        {
            var parameters = new Dictionary<string, object> {
        {"query", query.BuildParameters(true)},
        {"key", keyInQuery}
      };
            return new AVQuery<T>(this, where: new Dictionary<string, object> {
        {key, new Dictionary<string, object>{{"$select", parameters}}}
      });
        }

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value
        /// does not match any value for a key in the results of another AVQuery.
        /// </summary>
        /// <param name="key">The key whose value is being checked.</param>
        /// <param name="keyInQuery">The key in the objects from the subquery to look in.</param>
        /// <param name="query">The subquery to run</param>
        /// <returns>A new query with the additional constraint.</returns>
        public AVQuery<T> WhereDoesNotMatchesKeyInQuery<TOther>(string key,
          string keyInQuery,
          AVQuery<TOther> query) where TOther : AVObject
        {
            var parameters = new Dictionary<string, object> {
        {"query", query.BuildParameters(true)},
        {"key", keyInQuery}
      };
            return new AVQuery<T>(this, where: new Dictionary<string, object> {
        {key, new Dictionary<string, object>{{"$dontSelect", parameters}}}
      });
        }

        /// <summary>
        /// Adds a constraint to the query that requires that a particular key's value
        /// matches another AVQuery. This only works on keys whose values are
        /// ParseObjects or lists of ParseObjects.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="query">The query that the value should match.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public AVQuery<T> WhereMatchesQuery<TOther>(string key, AVQuery<TOther> query)
          where TOther : AVObject
        {
            return new AVQuery<T>(this, where: new Dictionary<string, object> {
        {key, new Dictionary<string, object>{{"$inQuery", query.BuildParameters(true)}}}
      });
        }

        /// <summary>
        /// Adds a proximity-based constraint for finding objects with keys whose GeoPoint
        /// values are near the given point.
        /// </summary>
        /// <param name="key">The key that the AVGeoPoint is stored in.</param>
        /// <param name="point">The reference AVGeoPoint.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public AVQuery<T> WhereNear(string key, AVGeoPoint point)
        {
            return new AVQuery<T>(this, where: new Dictionary<string, object> {
        {key, new Dictionary<string, object>{{"$nearSphere", point}}}
      });
        }

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value to be
        /// contained in the provided list of values.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="values">The values that will match.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public AVQuery<T> WhereNotContainedIn<TIn>(string key, IEnumerable<TIn> values)
        {
            return new AVQuery<T>(this, where: new Dictionary<string, object> {
        {key, new Dictionary<string, object>{{"$nin", values.ToList()}}}
      });
        }

        /// <summary>
        /// Adds a constraint to the query that requires a particular key's value not
        /// to be equal to the provided value.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="value">The value that that must not be equalled.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public AVQuery<T> WhereNotEqualTo(string key, object value)
        {
            return new AVQuery<T>(this, where: new Dictionary<string, object> {
        {key, new Dictionary<string, object>{{"$ne", value}}}
      });
        }

        /// <summary>
        /// Adds a constraint for finding string values that start with the provided string.
        /// This query will use the backend index, so it will be fast even with large data sets.
        /// </summary>
        /// <param name="key">The key that the string to match is stored in.</param>
        /// <param name="suffix">The substring that the value must start with.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public AVQuery<T> WhereStartsWith(string key, string suffix)
        {
            return new AVQuery<T>(this, where: new Dictionary<string, object> {
        {key, new Dictionary<string, object>{{"$regex", "^" + RegexQuote(suffix)}}}
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
        public AVQuery<T> WhereWithinGeoBox(string key,
          AVGeoPoint southwest,
          AVGeoPoint northeast)
        {
            return new AVQuery<T>(this, where: new Dictionary<string, object> {
        {
          key,
          new Dictionary<string, object> {
            {
              "$within",
              new Dictionary<string, object> {
                {"$box", new[] {southwest, northeast}}
              }
            }
          }
        }
      });
        }

        /// <summary>
        /// Adds a proximity-based constraint for finding objects with keys whose GeoPoint
        /// values are near the given point and within the maximum distance given.
        /// </summary>
        /// <param name="key">The key that the AVGeoPoint is stored in.</param>
        /// <param name="point">The reference AVGeoPoint.</param>
        /// <param name="maxDistance">The maximum distance (in radians) of results to return.</param>
        /// <returns>A new query with the additional constraint.</returns>
        public AVQuery<T> WhereWithinDistance(
            string key, AVGeoPoint point, AVGeoDistance maxDistance)
        {
            return new AVQuery<T>(WhereNear(key, point), where: new Dictionary<string, object> {
        {key, new Dictionary<string, object>{{"$maxDistance", maxDistance.Radians}}}
      });
        }

        internal AVQuery<T> WhereRelatedTo(AVObject parent, string key)
        {
            return new AVQuery<T>(this, where: new Dictionary<string, object> {
        {
          "$relatedTo",
          new Dictionary<string, object> {
            {"object", parent},
            {"key", key}
          }
        }
      });
        }

        #endregion

        /// <summary>
        /// Retrieves a list of ParseObjects that satisfy this query from LeanCloud.
        /// </summary>
        /// <returns>The list of ParseObjects that match this query.</returns>
        public Task<IEnumerable<T>> FindAsync()
        {
            return FindAsync(CancellationToken.None);
        }

        /// <summary>
        /// Retrieves a list of ParseObjects that satisfy this query from LeanCloud.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The list of ParseObjects that match this query.</returns>
        public Task<IEnumerable<T>> FindAsync(CancellationToken cancellationToken)
        {
            EnsureNotInstallationQuery();
            return QueryController.FindAsync<T>(this, AVUser.CurrentUser, cancellationToken).OnSuccess(t =>
            {
                IEnumerable<IObjectState> states = t.Result;

                return (from state in states
                        select AVObject.FromState<T>(state, ClassName));
            });
        }

        /// <summary>
        /// Retrieves at most one AVObject that satisfies this query.
        /// </summary>
        /// <returns>A single AVObject that satisfies this query, or else null.</returns>
        public Task<T> FirstOrDefaultAsync()
        {
            return FirstOrDefaultAsync(CancellationToken.None);
        }

        /// <summary>
        /// Retrieves at most one AVObject that satisfies this query.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A single AVObject that satisfies this query, or else null.</returns>
        public Task<T> FirstOrDefaultAsync(CancellationToken cancellationToken)
        {
            EnsureNotInstallationQuery();
            return QueryController.FirstAsync<T>(this, AVUser.CurrentUser, cancellationToken).OnSuccess(t =>
            {
                IObjectState state = t.Result;

                return state == null ? default(T) : AVObject.FromState<T>(state, ClassName);
            });
        }

        /// <summary>
        /// Retrieves at most one AVObject that satisfies this query.
        /// </summary>
        /// <returns>A single AVObject that satisfies this query.</returns>
        /// <exception cref="ParseException">If no results match the query.</exception>
        public Task<T> FirstAsync()
        {
            return FirstAsync(CancellationToken.None);
        }

        /// <summary>
        /// Retrieves at most one AVObject that satisfies this query.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A single AVObject that satisfies this query.</returns>
        /// <exception cref="ParseException">If no results match the query.</exception>
        public Task<T> FirstAsync(CancellationToken cancellationToken)
        {
            return FirstOrDefaultAsync(cancellationToken).OnSuccess(t =>
            {
                if (t.Result == null)
                {
                    throw new AVException(AVException.ErrorCode.ObjectNotFound,
                      "No results matched the query.");
                }
                return t.Result;
            });
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
            return QueryController.CountAsync<T>(this, AVUser.CurrentUser, cancellationToken);
        }

        /// <summary>
        /// Constructs a AVObject whose id is already known by fetching data
        /// from the server.
        /// </summary>
        /// <param name="objectId">ObjectId of the AVObject to fetch.</param>
        /// <returns>The AVObject for the given objectId.</returns>
        public Task<T> GetAsync(string objectId)
        {
            return GetAsync(objectId, CancellationToken.None);
        }

        /// <summary>
        /// Constructs a AVObject whose id is already known by fetching data
        /// from the server.
        /// </summary>
        /// <param name="objectId">ObjectId of the AVObject to fetch.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The AVObject for the given objectId.</returns>
        public Task<T> GetAsync(string objectId, CancellationToken cancellationToken)
        {
            AVQuery<T> singleItemQuery = new AVQuery<T>(className).
                WhereEqualTo("objectId", objectId);
            singleItemQuery = new AVQuery<T>(singleItemQuery, includes: this.includes, selectedKeys: this.selectedKeys, limit: 1);
            return singleItemQuery.FindAsync(cancellationToken).OnSuccess(t =>
            {
                var result = t.Result.FirstOrDefault();
                if (result == null)
                {
                    throw new AVException(AVException.ErrorCode.ObjectNotFound,
                      "Object with the given objectId not found.");
                }
                return result;
            });
        }

        #region CQL

        /// <summary>
        /// 执行 CQL 查询
        /// </summary>
        /// <param name="cql">CQL 语句</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>返回符合条件的对象集合</returns>
        public static Task<IEnumerable<T>> DoCloudQueryAsync(string cql, CancellationToken cancellationToken)
        {
            var queryString = string.Format("cloudQuery?cql={0}", Uri.EscapeDataString(cql));

            return rebuildObjectFromCloudQueryResult(queryString);
        }

        /// <summary>
        /// 执行 CQL 查询
        /// </summary>
        /// <param name="cql"></param>
        /// <returns></returns>
        public static Task<IEnumerable<T>> DoCloudQueryAsync(string cql)
        {
            return DoCloudQueryAsync(cql, CancellationToken.None);
        }

        public static Task<IEnumerable<T>> DoCloudQueryAsync(string cqlTeamplate, params object[] pvalues)
        {
            string queryStringTemplate = "cloudQuery?cql={0}&pvalues={1}";
            string pSrting = Json.Encode(pvalues);
            string queryString = string.Format(queryStringTemplate, Uri.EscapeDataString(cqlTeamplate), Uri.EscapeDataString(pSrting));

            return rebuildObjectFromCloudQueryResult(queryString);
        }

        internal static Task<IEnumerable<T>> rebuildObjectFromCloudQueryResult(string queryString)
        {
            var command = new AVCommand(queryString,
              method: "GET",
              sessionToken: AVUser.CurrentSessionToken,
              data: null);

            return AVPlugins.Instance.CommandRunner.RunCommandAsync(command, cancellationToken: CancellationToken.None).OnSuccess(t =>
            {
                var items = t.Result.Item2["results"] as IList<object>;
                var className = t.Result.Item2["className"].ToString();

                IEnumerable<IObjectState> states = (from item in items
                                                    select AVObjectCoder.Instance.Decode(item as IDictionary<string, object>, AVDecoder.Instance));

                return (from state in states
                        select AVObject.FromState<T>(state, className));
            });
        }



        #endregion

        internal object GetConstraint(string key)
        {
            return where == null ? null : where.GetOrDefault(key, null);
        }

        internal IDictionary<string, object> BuildParameters(bool includeClassName = false)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (where != null)
            {
                result["where"] = PointerOrLocalIdEncoder.Instance.Encode(where);
            }
            if (orderBy != null)
            {
                result["order"] = string.Join(",", orderBy.ToArray());
            }
            if (skip != null)
            {
                result["skip"] = skip.Value;
            }
            if (limit != null)
            {
                result["limit"] = limit.Value;
            }
            if (includes != null)
            {
                result["include"] = string.Join(",", includes.ToArray());
            }
            if (selectedKeys != null)
            {
                result["keys"] = string.Join(",", selectedKeys.ToArray());
            }
            if (includeClassName)
            {
                result["className"] = className;
            }
            if (redirectClassNameForKey != null)
            {
                result["redirectClassNameForKey"] = redirectClassNameForKey;
            }
            return result;
        }

        private string RegexQuote(string input)
        {
            return "\\Q" + input.Replace("\\E", "\\E\\\\E\\Q") + "\\E";
        }

        private string GetRegexOptions(Regex regex, string modifiers)
        {
            string result = modifiers ?? "";
            if (regex.Options.HasFlag(RegexOptions.IgnoreCase) && !modifiers.Contains("i"))
            {
                result += "i";
            }
            if (regex.Options.HasFlag(RegexOptions.Multiline) && !modifiers.Contains("m"))
            {
                result += "m";
            }
            return result;
        }

        private IDictionary<string, object> EncodeRegex(Regex regex, string modifiers)
        {
            var options = GetRegexOptions(regex, modifiers);
            var dict = new Dictionary<string, object>();
            dict["$regex"] = regex.ToString();
            if (!string.IsNullOrEmpty(options))
            {
                dict["$options"] = options;
            }
            return dict;
        }

        private void EnsureNotInstallationQuery()
        {
            // The ParseInstallation class is not accessible from this project; using string literal.
            if (className.Equals("_Installation"))
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
            if (obj == null || !(obj is AVQuery<T>))
            {
                return false;
            }

            var other = obj as AVQuery<T>;
            return Object.Equals(this.className, other.ClassName) &&
                   this.where.CollectionsEqual(other.where) &&
                   this.orderBy.CollectionsEqual(other.orderBy) &&
                   this.includes.CollectionsEqual(other.includes) &&
                   this.selectedKeys.CollectionsEqual(other.selectedKeys) &&
                   Object.Equals(this.skip, other.skip) &&
                   Object.Equals(this.limit, other.limit);
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
