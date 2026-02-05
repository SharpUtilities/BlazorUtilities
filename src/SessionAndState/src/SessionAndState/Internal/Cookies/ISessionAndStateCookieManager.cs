using Microsoft.AspNetCore.Http;

namespace SessionAndState.Internal.Cookies;

/// <summary>
/// Handles reading and writing session keys to/from cookies.
/// Centralizes all cookie operations for anonymous sessions.
/// </summary>
internal interface ISessionAndStateCookieManager
{
    /// <summary>
    /// Attempts to read and decrypt a session key from the cookie.
    /// Returns null if no cookie exists or decryption fails.
    /// </summary>
    string? ReadSessionKey(HttpContext context);

    /// <summary>
    /// Encrypts and writes the session key to a cookie.
    /// </summary>
    void WriteSessionKey(HttpContext context, string key);

    /// <summary>
    /// Deletes the session cookie.
    /// </summary>
    void DeleteSessionKey(HttpContext context);
}
