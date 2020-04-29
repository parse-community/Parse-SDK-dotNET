// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure.Data;
using Parse.Abstractions.Infrastructure.Execution;
using Parse.Abstractions.Platform.Objects;
using Parse.Abstractions.Platform.Queries;
using Parse.Infrastructure.Data;
using Parse.Infrastructure.Execution;
using Parse.Infrastructure.Utilities;

namespace Parse.Platform.Queries
{
    /// <summary>
    /// A straightforward implementation of <see cref="IParseQueryController"/> that uses <see cref="ParseObject.Services"/> to decode raw server data when needed.
    /// </summary>
    internal class ParseQueryController : IParseQueryController
    {
        IParseCommandRunner CommandRunner { get; }

        IParseDataDecoder Decoder { get; }

        public ParseQueryController(IParseCommandRunner commandRunner, IParseDataDecoder decoder) => (CommandRunner, Decoder) = (commandRunner, decoder);

        public Task<IEnumerable<IObjectState>> FindAsync<T>(ParseQuery<T> query, ParseUser user, CancellationToken cancellationToken = default) where T : ParseObject => FindAsync(query.ClassName, query.BuildParameters(), user?.SessionToken, cancellationToken).OnSuccess(t => (from item in t.Result["results"] as IList<object> select ParseObjectCoder.Instance.Decode(item as IDictionary<string, object>, Decoder, user.Services)));

        public Task<int> CountAsync<T>(ParseQuery<T> query, ParseUser user, CancellationToken cancellationToken = default) where T : ParseObject
        {
            IDictionary<string, object> parameters = query.BuildParameters();
            parameters["limit"] = 0;
            parameters["count"] = 1;

            return FindAsync(query.ClassName, parameters, user?.SessionToken, cancellationToken).OnSuccess(task => Convert.ToInt32(task.Result["count"]));
        }

        public Task<IObjectState> FirstAsync<T>(ParseQuery<T> query, ParseUser user, CancellationToken cancellationToken = default) where T : ParseObject
        {
            IDictionary<string, object> parameters = query.BuildParameters();
            parameters["limit"] = 1;

            return FindAsync(query.ClassName, parameters, user?.SessionToken, cancellationToken).OnSuccess(task => (task.Result["results"] as IList<object>).FirstOrDefault() as IDictionary<string, object> is Dictionary<string, object> item && item != null ? ParseObjectCoder.Instance.Decode(item, Decoder, user.Services) : null);
        }

        Task<IDictionary<string, object>> FindAsync(string className, IDictionary<string, object> parameters, string sessionToken, CancellationToken cancellationToken = default) => CommandRunner.RunCommandAsync(new ParseCommand($"classes/{Uri.EscapeDataString(className)}?{ParseClient.BuildQueryString(parameters)}", method: "GET", sessionToken: sessionToken, data: null), cancellationToken: cancellationToken).OnSuccess(t => t.Result.Item2);
    }
}
