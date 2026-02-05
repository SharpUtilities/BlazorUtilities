namespace SessionAndState.Internal;

/// <summary>
/// Internal time provider for SessionAndState.
/// Inherits from TimeProvider for testability while being isolated from
/// any custom TimeProvider the application may have registered.
/// </summary>
internal class SessionAndStateTimeProvider : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;
}
