// Copyright (c) 2015-present, Parse, LLC.  All rights reserved.  This source code is licensed under the BSD-style license found in the LICENSE file in the root directory of this source tree.  An additional grant of patent rights can be found in the PATENTS file in the same directory.

using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Infrastructure.Utilities;

namespace Parse
{
    public static class SessionsServiceExtensions
    {
        /// <summary>
        /// Constructs a <see cref="ParseQuery{ParseSession}"/> for ParseSession.
        /// </summary>
        public static ParseQuery<ParseSession> GetSessionQuery(this IServiceHub serviceHub) => serviceHub.GetQuery<ParseSession>();

        /// <summary>
        /// Gets the current <see cref="ParseSession"/> object related to the current user.
        /// </summary>
        public static Task<ParseSession> GetCurrentSessionAsync(this IServiceHub serviceHub) => GetCurrentSessionAsync(serviceHub, CancellationToken.None);

        /// <summary>
        /// Gets the current <see cref="ParseSession"/> object related to the current user.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token</param>
        public static Task<ParseSession> GetCurrentSessionAsync(this IServiceHub serviceHub, CancellationToken cancellationToken) => serviceHub.GetCurrentUserAsync().OnSuccess(task => task.Result switch
        {
            null => Task.FromResult<ParseSession>(default),
            { SessionToken: null } => Task.FromResult<ParseSession>(default),
            { SessionToken: { } sessionToken } => serviceHub.SessionController.GetSessionAsync(sessionToken, serviceHub, cancellationToken).OnSuccess(successTask => serviceHub.GenerateObjectFromState<ParseSession>(successTask.Result, "_Session"))
        }).Unwrap();

        public static Task RevokeSessionAsync(this IServiceHub serviceHub, string sessionToken, CancellationToken cancellationToken) => sessionToken is null || !serviceHub.SessionController.IsRevocableSessionToken(sessionToken) ? Task.CompletedTask : serviceHub.SessionController.RevokeAsync(sessionToken, cancellationToken);

        public static Task<string> UpgradeToRevocableSessionAsync(this IServiceHub serviceHub, string sessionToken, CancellationToken cancellationToken) => sessionToken is null || serviceHub.SessionController.IsRevocableSessionToken(sessionToken) ? Task.FromResult(sessionToken) : serviceHub.SessionController.UpgradeToRevocableSessionAsync(sessionToken, serviceHub, cancellationToken).OnSuccess(task => serviceHub.GenerateObjectFromState<ParseSession>(task.Result, "_Session").SessionToken);
    }
}
