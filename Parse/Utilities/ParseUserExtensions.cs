using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Parse.Abstractions.Internal
{
    /// <summary>
    /// So here's the deal. We have a lot of internal APIs for ParseObject, ParseUser, etc.
    ///
    /// These cannot be 'internal' anymore if we are fully modularizing things out, because
    /// they are no longer a part of the same library, especially as we create things like
    /// Installation inside push library.
    ///
    /// So this class contains a bunch of extension methods that can live inside another
    /// namespace, which 'wrap' the intenral APIs that already exist.
    /// </summary>
    public static class ParseUserExtensions
    {
        public static Task UnlinkFromAsync(this ParseUser user, string authType, CancellationToken cancellationToken)
        {
            return user.UnlinkFromAsync(authType, cancellationToken);
        }

        public static Task LinkWithAsync(this ParseUser user, string authType, CancellationToken cancellationToken)
        {
            return user.LinkWithAsync(authType, cancellationToken);
        }

        public static Task LinkWithAsync(this ParseUser user, string authType, IDictionary<string, object> data, CancellationToken cancellationToken)
        {
            return user.LinkWithAsync(authType, data, cancellationToken);
        }

        public static Task UpgradeToRevocableSessionAsync(this ParseUser user, CancellationToken cancellationToken)
        {
            return user.UpgradeToRevocableSessionAsync(cancellationToken);
        }
    }
}
