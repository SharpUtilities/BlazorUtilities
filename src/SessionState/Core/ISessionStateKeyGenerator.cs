using Microsoft.AspNetCore.Http;

namespace SessionState.Core;

/// <summary>
/// Generates or resolves session keys.
/// Implement this interface to provide custom key generation logic.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Warning:</strong> If you return a user-specific identifier (e.g., user ID),
/// the session state will be shared across ALL browser tabs, devices, and sessions
/// for that user. This may be desirable for user preferences but problematic for
/// per-session data.
/// </para>
/// </remarks>
public interface ISessionStateKeyGenerator
{
    ValueTask<string> GenerateNonAuthenticatedKeyAsync(HttpContext httpContext, CancellationToken cancellationToken = default);

    // nonAuthenticatedKey will be null if there is no existing anonymous session key
    ValueTask<string> GenerateAuthenticatedKeyAsync(SessionStateKeyContext sessionStateKeyContext, string? nonAuthenticatedKey, CancellationToken cancellationToken = default);
}
