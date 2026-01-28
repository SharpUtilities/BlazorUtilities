namespace SessionState.Core;

/// <summary>
/// Backend storage abstraction for SessionState.
/// Implementations handle the actual persistence of state data.
/// </summary>
/// <remarks>
/// Backend implementations should be pure storage - they should not raise events.
/// Events are handled by the SessionState infrastructure.
/// </remarks>
public interface ISessionStateBackend
{
    /// <summary>
    /// Retrieves a value from storage.
    /// </summary>
    ValueTask<T?> GetAsync<T>(string sessionKey, CancellationToken cancellationToken = default)
        where T : class, ISessionStateType<T>;

    /// <summary>
    /// Stores a value.
    /// </summary>
    ValueTask SetAsync<T>(string sessionKey, T value, CancellationToken cancellationToken = default)
        where T : class, ISessionStateType<T>;

    /// <summary>
    /// Removes a specific value.
    /// </summary>
    ValueTask RemoveAsync<T>(string sessionKey, CancellationToken cancellationToken = default)
        where T : class, ISessionStateType<T>;

    /// <summary>
    /// Refreshes the expiry of a specific value.
    /// </summary>
    ValueTask RefreshAsync<T>(string sessionKey, CancellationToken cancellationToken = default)
        where T : class, ISessionStateType<T>;

    /// <summary>
    /// Removes all expired entries and returns information about what was removed.
    /// </summary>
    ValueTask<IReadOnlyList<ExpiredSessionStateInfo>> RemoveExpiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all state for a given session key.
    /// </summary>
    ValueTask RemoveAllForKeyAsync(string sessionKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Migrates all session data from one key to another, removing the old entries.
    /// Used when transitioning from anonymous to authenticated sessions.
    /// This operation should be idempotent - if the source key doesn't exist, it should succeed silently.
    /// </summary>
    /// <param name="sourceKey">The source session key (anonymous session).</param>
    /// <param name="destinationKey">The destination session key (authenticated session).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask MigrateSessionAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default);
}
