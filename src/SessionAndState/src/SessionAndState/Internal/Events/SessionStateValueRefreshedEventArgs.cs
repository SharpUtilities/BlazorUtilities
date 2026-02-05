namespace SessionAndState.Internal.Events;

/// <summary>
/// Event arguments for when a value is refreshed.
/// </summary>
public sealed class SessionStateValueRefreshedEventArgs : SessionStateEventArgs
{
    /// <summary>
    /// The new expiry time after refresh.
    /// </summary>
    public DateTimeOffset? NewExpiry { get; init; }
}
