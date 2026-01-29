namespace BlazorState.Core.Options;

/// <summary>
/// Configuration options for BlazorState.
/// </summary>
public sealed class BlazorStateOptions
{
    /// <summary>
    /// The claim type used to store the session key for authenticated users.
    /// Default: "blazor_session_state_key"
    /// </summary>
    public string ClaimType { get; set; } = "blazor_session_state_key";

    /// <summary>
    /// The cookie name used to store the session key for anonymous users.
    /// Default: ".BlazorState"
    /// </summary>
    public string CookieName { get; set; } = ".BlazorState";

    /// <summary>
    /// The maximum age for the anonymous session cookie.
    /// Default: 30 days
    /// </summary>
    public Expiration CookieExpiration { get; set; } = Expiration.AfterMinutes(30);

    /// <summary>
    /// The maximum age for the anonymous session cookie.
    /// Default: 30 days
    /// </summary>
    public TimeSpan CookieMaxAge { get; set; } = TimeSpan.FromDays(30);

    /// <summary>
    /// The Data Protection purpose string used to encrypt session keys in cookies.
    /// Default: "BlazorState.Key"
    /// </summary>
    public string DataProtectionPurpose { get; set; } = "BlazorState.Key";

    /// <summary>
    /// Interval between cleanup runs for expired session state.
    /// Default: 5 minutes
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// When true, session state is only available for authenticated users.
    /// Anonymous requests will not have a session key created.
    /// Default: false
    /// </summary>
    public bool RequireAuthentication { get; set; } = false;

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
