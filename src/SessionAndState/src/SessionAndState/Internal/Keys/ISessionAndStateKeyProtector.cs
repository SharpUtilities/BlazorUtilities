namespace SessionAndState.Internal.Keys;

/// <summary>
/// Encrypts and decrypts session key for cookie storage.
/// </summary>
internal interface ISessionAndStateKeyProtector
{
    /// <summary>
    /// Encrypts a session key for safe storage in a cookie.
    /// </summary>
    string Protect(string sessionKey);

    /// <summary>
    /// Decrypts a protected value back to a session key
    /// Returns null if the value is invalid or tampered with.
    /// </summary>
    string? Unprotect(string protectedValue);
}
