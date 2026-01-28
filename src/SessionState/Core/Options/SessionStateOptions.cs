namespace SessionState.Core.Options;

/// <summary>
/// Configuration options for SessionState.
/// </summary>
public sealed class SessionStateOptions
{
    /// <summary>
    /// The claim type used to store the session key for authenticated users.
    /// Default: "blazor_session_state_key"
    /// </summary>
    public string ClaimType { get; set; } = "blazor_session_state_key";

    /// <summary>
    /// The cookie name used to store the session key for anonymous users.
    /// Default: ".SessionState"
    /// </summary>
    public string CookieName { get; set; } = ".SessionState";

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
    /// Default: "SessionState.Key"
    /// </summary>
    public string DataProtectionPurpose { get; set; } = "SessionState.Key";

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
}
