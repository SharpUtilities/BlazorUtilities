namespace SessionAndState.Core.Options;

/// <summary>
/// Enables storing the session key in the auth cookie as a claim (auth transport).
/// </summary>
public sealed class AuthCookieClaimSessionKeyOptions
{
    /// <summary>
    /// The claim type used to store the session key.
    /// Default: "session_and_state_key"
    /// </summary>
    public string ClaimType { get; set; } = "session_and_state_key";
}
