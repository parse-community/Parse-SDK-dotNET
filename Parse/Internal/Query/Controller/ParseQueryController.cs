// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Parse.Common.Internal;

namespace Parse.Core.Internal
{
    internal class ParseQueryController : IParseQueryController
    {
        private readonly IParseCommandRunner commandRunner;

        public ParseQueryController(IParseCommandRunner commandRunner) => this.commandRunner = commandRunner;

        public Task<IEnumerable<IObjectState>> FindAsync<T>(ParseQuery<T> query, ParseUser user, CancellationToken cancellationToken) where T : ParseObject => FindAsync(query.ClassName, query.BuildParameters(), user?.SessionToken, cancellationToken).OnSuccess(t => (from item in t.Result["results"] as IList<object> select ParseObjectCoder.Instance.Decode(item as IDictionary<string, object>, ParseDecoder.Instance)));

        public Task<int> CountAsync<T>(ParseQuery<T> query, ParseUser user, CancellationToken cancellationToken) where T : ParseObject
        {
            var parameters = query.BuildParameters();
            parameters["limit"] = 0;
            parameters["count"] = 1;

            return FindAsync(query.ClassName, parameters, user?.SessionToken, cancellationToken).OnSuccess(t => Convert.ToInt32(t.Result["count"]));
        }

        public Task<IObjectState> FirstAsync<T>(ParseQuery<T> query, ParseUser user, CancellationToken cancellationToken) where T : ParseObject
        {
            var parameters = query.BuildParameters();
            parameters["limit"] = 1;

            return FindAsync(query.ClassName, parameters, user?.SessionToken, cancellationToken).OnSuccess(t => (t.Result["results"] as IList<object>).FirstOrDefault() as IDictionary<string, object> is Dictionary<string, object> item && item != null ? ParseObjectCoder.Instance.Decode(item, ParseDecoder.Instance) : null);
        }

        private Task<IDictionary<string, object>> FindAsync(string className, IDictionary<string, object> parameters, string sessionToken, CancellationToken cancellationToken) => commandRunner.RunCommandAsync(new ParseCommand($"classes/{Uri.EscapeDataString(className)}?{ParseClient.BuildQueryString(parameters)}", method: "GET", sessionToken: sessionToken, data: null), cancellationToken: cancellationToken).OnSuccess(t => t.Result.Item2);
    }
}
