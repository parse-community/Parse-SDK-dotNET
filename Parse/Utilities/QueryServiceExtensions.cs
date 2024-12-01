using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Parse.Abstractions.Infrastructure;

namespace Parse
{
    public static class QueryServiceExtensions
    {
        public static ParseQuery<T> GetQuery<T>(this IServiceHub serviceHub) where T : ParseObject
        {
            return new ParseQuery<T>(serviceHub);
        }

        // ALTERNATE NAME: BuildOrQuery

        /// <summary>
        /// Constructs a query that is the or of the given queries.
        /// </summary>
        /// <typeparam name="T">The type of ParseObject being queried.</typeparam>
        /// <param name="source">An initial query to 'or' with additional queries.</param>
        /// <param name="queries">The list of ParseQueries to 'or' together.</param>
        /// <returns>A query that is the or of the given queries.</returns>
        public static ParseQuery<T> ConstructOrQuery<T>(this IServiceHub serviceHub, ParseQuery<T> source, params ParseQuery<T>[] queries) where T : ParseObject
        {
            return serviceHub.ConstructOrQuery(queries.Concat(new[] { source }));
        }

        /// <summary>
        /// Constructs a query that is the or of the given queries.
        /// </summary>
        /// <param name="queries">The list of ParseQueries to 'or' together.</param>
        /// <returns>A ParseQquery that is the 'or' of the passed in queries.</returns>
        public static ParseQuery<T> ConstructOrQuery<T>(this IServiceHub serviceHub, IEnumerable<ParseQuery<T>> queries) where T : ParseObject
        {
            string className = default;
            List<IDictionary<string, object>> orValue = new List<IDictionary<string, object>> { };

            // We need to cast it to non-generic IEnumerable because of AOT-limitation

            IEnumerable nonGenericQueries = queries;
            foreach (object obj in nonGenericQueries)
            {
                ParseQuery<T> query = obj as ParseQuery<T>;

                if (className is { } && query.ClassName != className)
                {
                    throw new ArgumentException("All of the queries in an or query must be on the same class.");
                }

                className = query.ClassName;
                IDictionary<string, object> parameters = query.BuildParameters();

                if (parameters.Count == 0)
                {
                    continue;
                }

                if (!parameters.TryGetValue("where", out object where) || parameters.Count > 1)
                {
                    throw new ArgumentException("None of the queries in an or query can have non-filtering clauses");
                }

                orValue.Add(where as IDictionary<string, object>);
            }

            return new ParseQuery<T>(new ParseQuery<T>(serviceHub, className), where: new Dictionary<string, object> { ["$or"] = orValue });
        }
    }
}
