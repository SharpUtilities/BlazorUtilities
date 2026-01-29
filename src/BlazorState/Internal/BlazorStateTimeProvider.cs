namespace BlazorState.Internal;

/// <summary>
/// Internal time provider for BlazorState.
/// Inherits from TimeProvider for testability while being isolated from
/// any custom TimeProvider the application may have registered.
/// </summary>
internal class BlazorStateTimeProvider : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;
}
