using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;
using Parse.Infrastructure.Utilities;

namespace Parse;

public static class SessionsServiceExtensions
{
    /// <summary>
    /// Constructs a <see cref="ParseQuery{ParseSession}"/> for ParseSession.
    /// </summary>
    public static ParseQuery<ParseSession> GetSessionQuery(this IServiceHub serviceHub)
    {
        return serviceHub.GetQuery<ParseSession>();
    }

    /// <summary>
    /// Gets the current <see cref="ParseSession"/> object related to the current user.
    /// </summary>
    public static Task<ParseSession> GetCurrentSessionAsync(this IServiceHub serviceHub)
    {
        return GetCurrentSessionAsync(serviceHub, CancellationToken.None);
    }

    /// <summary>
    /// Gets the current <see cref="ParseSession"/> object related to the current user.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    public static async Task<ParseSession> GetCurrentSessionAsync(this IServiceHub serviceHub, CancellationToken cancellationToken)
    {
        var currentUser = await serviceHub.GetCurrentUserAsync().ConfigureAwait(false);

        if (currentUser == null || currentUser.SessionToken == null)
        {
            // Return null if there is no current user or session token
            return null;
        }

        // Fetch the session using the session token
        var sessionState = await serviceHub.SessionController
            .GetSessionAsync(currentUser.SessionToken, serviceHub, cancellationToken)
            .ConfigureAwait(false);

        // Generate and return the ParseSession object
        return serviceHub.GenerateObjectFromState<ParseSession>(sessionState, "_Session");
    }


    public static Task RevokeSessionAsync(this IServiceHub serviceHub, string sessionToken, CancellationToken cancellationToken)
    {
        return sessionToken is null || !serviceHub.SessionController.IsRevocableSessionToken(sessionToken) ? Task.CompletedTask : serviceHub.SessionController.RevokeAsync(sessionToken, cancellationToken);
    }

    public static async Task<string> UpgradeToRevocableSessionAsync(this IServiceHub serviceHub, string sessionToken, CancellationToken cancellationToken)
    {
        if (sessionToken is null || serviceHub.SessionController.IsRevocableSessionToken(sessionToken))
        {
            return sessionToken;
        }

        // Perform the upgrade asynchronously
        var upgradeResult = await serviceHub.SessionController.UpgradeToRevocableSessionAsync(sessionToken, serviceHub, cancellationToken).ConfigureAwait(false);

        // Generate the session object from the result and return the session token
        var session = serviceHub.GenerateObjectFromState<ParseSession>(upgradeResult, "_Session");
        return session.SessionToken;
    }

}
