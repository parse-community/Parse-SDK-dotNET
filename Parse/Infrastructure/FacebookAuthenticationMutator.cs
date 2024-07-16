using System;
using System.Collections.Generic;
using System.Text;

using Parse.Abstractions.Infrastructure;
using Parse.Infrastructure.Utilities;

namespace Parse.Infrastructure
{
    /// <summary>
    /// Enables Facebook authentication on the service hub.
    /// </summary>
    public class FacebookAuthenticationMutator : IServiceHubMutator
    {
        /// <inheritdoc/>
        public bool Valid => Caller is { Length: > 0 };

        /// <summary>
        /// The identifier to make API calls to Facebook with. This is in the Facebook Developer Dashboard as some variant of "Application ID" or "API Key".
        /// </summary>
        public string Caller { get; set; }

        /// <inheritdoc/>
        public void Mutate(ref IMutableServiceHub mutableHub, in IServiceHub consumableHub, Stack<IServiceHubMutator> futureMutators) => mutableHub.InitializeFacebookAuthenticationProvider(Caller);
    }
}
