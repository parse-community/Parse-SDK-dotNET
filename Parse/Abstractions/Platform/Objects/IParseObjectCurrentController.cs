using System.Threading;
using System.Threading.Tasks;
using Parse.Abstractions.Infrastructure;

namespace Parse.Abstractions.Platform.Objects;

/// <summary>
/// <code>IParseObjectCurrentController</code> controls the single-instance <see cref="ParseObject"/>
/// persistence used throughout the code-base. Sample usages are <see cref="ParseUser.CurrentUser"/> and
/// <see cref="ParseInstallation.CurrentInstallation"/>.
/// </summary>
/// <typeparam name="T">Type of object being persisted.</typeparam>
public interface IParseObjectCurrentController<T> where T : ParseObject
{
    /// <summary>
    /// Persists current <see cref="ParseObject"/>.
    /// </summary>
    /// <param name="obj"><see cref="ParseObject"/> to be persisted.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SetAsync(T obj, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the persisted current <see cref="ParseObject"/>.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<T> GetAsync(IServiceHub serviceHub, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a <see cref="Task"/> that resolves to <code>true</code> if current
    /// <see cref="ParseObject"/> exists.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<bool> ExistsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <code>true</code> if the given <see cref="ParseObject"/> is the persisted current
    /// <see cref="ParseObject"/>.
    /// </summary>
    /// <param name="obj">The object to check.</param>
    /// <returns>true if <code>obj</code> is the current persisted <see cref="ParseObject"/>.</returns>
    bool IsCurrent(T obj);

    /// <summary>
    /// Nullifies the current <see cref="ParseObject"/> from memory.
    /// </summary>
    void ClearFromMemory();

    /// <summary>
    /// Clears current <see cref="ParseObject"/> from disk.
    /// </summary>
    Task ClearFromDiskAsync();
}
