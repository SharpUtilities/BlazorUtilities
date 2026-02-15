namespace SessionAndState.Core.Options;

/// <summary>
/// Configuration options for SessionAndState.
/// </summary>
public sealed class SessionAndStateOptions
{
    /// <summary>
    /// The Data Protection purpose string used to encrypt session keys in cookies.
    /// Default: "SessionAndState.Key"
    /// </summary>
    public string DataProtectionPurpose { get; set; } = "SessionAndState.Key";

    /// <summary>
    /// Maximum allowed age of a protected (encrypted) anonymous session cookie payload.
    /// This is used when decrypting the cookie to reject old cookies.
    /// Default: 30 days
    /// </summary>
    public TimeSpan ProtectedSessionKeyMaxAge { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Interval between cleanup runs for expired session state.
    /// Default: 5 minutes
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Whether to track property changes on stored objects.
    /// Default: true
    /// </summary>
    public bool TrackPropertyChanges { get; set; } = true;

    /// <summary>
    /// Whether to automatically persist changes when a property changes.
    /// Default: true
    /// </summary>
    public bool AutoPersistOnPropertyChange { get; set; } = true;
}
