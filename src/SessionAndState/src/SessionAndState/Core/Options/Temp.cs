using Microsoft.AspNetCore.Http;

namespace SessionAndState.Core.Options;

/// <summary>
/// Enables storing the session key in an encrypted cookie (anonymous/bootstrap transport).
/// Inherits from CookieOptions so callers can configure expiry/max-age, same-site, etc.
/// </summary>
public sealed class AnonymousCookieSessionOptions : CookieOptions
{
    /// <summary>
    /// The cookie name used to store the session key.
    /// Default: ".SessionAndState"
    /// </summary>
    public string CookieName { get; set; } = ".SessionAndState";
}

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
