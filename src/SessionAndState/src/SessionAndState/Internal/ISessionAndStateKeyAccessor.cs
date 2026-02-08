namespace SessionAndState.Internal;

/// <summary>
/// Provides read-only access to the current session key.
/// </summary>
/// <remarks>
/// <para>
/// This is the primary way to access the session key in your application.
/// The key is automatically established by the SessionAndState middleware.
/// </para>
/// <para>
/// For anonymous users, a key is generated and stored in an encrypted cookie.
/// For authenticated users, the key is stored as a claim in the authentication cookie.
/// </para>
/// </remarks>
internal interface ISessionAndStateKeyAccessor
{
    /// <summary>
    /// Gets the current session key, or null if not established.
    /// </summary>
    /// <remarks>
    /// Returns null when:
    /// <list type="bullet">
    ///   <item>The middleware hasn't run yet</item>
    ///   <item>RequireAuthentication is true and the user is not authenticated</item>
    /// </list>
    /// </remarks>
    string? SessionKey { get; }

    /// <summary>
    /// Whether a session key has been established for this request.
    /// </summary>
    bool HasSessionKey { get; }

    /// <summary>
    /// Whether the current session belongs to an authenticated user.
    /// </summary>
    bool IsUsingAuthenticatedSessionKey { get; }
}
