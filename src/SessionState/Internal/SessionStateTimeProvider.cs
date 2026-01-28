namespace SessionState.Internal;

/// <summary>
/// Internal time provider for SessionState.
/// Inherits from TimeProvider for testability while being isolated from
/// any custom TimeProvider the application may have registered.
/// </summary>
internal class SessionStateTimeProvider : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;
}
