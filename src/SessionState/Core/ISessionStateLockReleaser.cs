namespace SessionState.Core;

/// <summary>
/// Internal interface for releasing locks.
/// </summary>
internal interface ISessionStateLockReleaser
{
    /// <summary>
    /// Releases a lock by name.
    /// </summary>
    /// <param name="name">The name of the lock to release.</param>
    void ReleaseLock(string name);
}
