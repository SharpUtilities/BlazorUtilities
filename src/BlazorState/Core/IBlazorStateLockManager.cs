namespace BlazorState.Core;

/// <summary>
/// Provides named locking within a session scope.
/// Useful for preventing concurrent operations on the same resource.
/// </summary>
public interface IBlazorStateLockManager
{
    /// <summary>
    /// Attempts to acquire a named lock. Returns immediately without blocking.
    /// </summary>
    /// <param name="name">The name of the lock to acquire.</param>
    /// <param name="lockHandle">The lock handle if acquired. Dispose or call Release() to release.</param>
    /// <returns>True if the lock was acquired, false if already held.</returns>
    bool TryAcquireLock(string name, out BlazorStateLock lockHandle);

    /// <summary>
    /// Attempts to acquire a named lock, waiting up to the specified timeout.
    /// </summary>
    /// <param name="name">The name of the lock to acquire.</param>
    /// <param name="timeout">Maximum time to wait for the lock.</param>
    /// <param name="lockHandle">The lock handle if acquired. Dispose or call Release() to release.</param>
    /// <returns>True if the lock was acquired, false if timed out.</returns>
    bool TryAcquireLock(string name, TimeSpan timeout, out BlazorStateLock lockHandle);

    /// <summary>
    /// Acquires a named lock asynchronously, waiting until available or cancelled.
    /// </summary>
    /// <param name="name">The name of the lock to acquire.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The lock handle. Dispose or call Release() to release.</returns>
    ValueTask<BlazorStateLock> AcquireLockAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to acquire a named lock asynchronously, waiting up to the specified timeout.
    /// </summary>
    /// <param name="name">The name of the lock to acquire.</param>
    /// <param name="timeout">Maximum time to wait for the lock.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The lock handle. Check <see cref="BlazorStateLock.IsAcquired"/> to see if successful.</returns>
    ValueTask<BlazorStateLock> TryAcquireLockAsync(string name, TimeSpan timeout, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a named lock is currently held.
    /// </summary>
    /// <param name="name">The name of the lock to check.</param>
    /// <returns>True if the lock is currently held.</returns>
    bool IsLocked(string name);
}
