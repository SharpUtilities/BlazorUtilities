using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace BlazorState.Internal.Session;

/// <summary>
/// Manages the session lifecycle - establishing, transitioning, and clearing sessions.
/// This is the central coordinator for all session operations.
/// </summary>
internal interface IBlazorStateSessionManager
{
    /// <summary>
    /// Establishes a session for the current request using IHttpContextAccessor.
    /// </summary>
    ValueTask EstablishSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Establishes a session for the current request using the provided HttpContext.
    /// Uses HttpContext.User for the principal.
    /// </summary>
    ValueTask EstablishSessionAsync(HttpContext httpContext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Establishes a session using the provided HttpContext and ClaimsPrincipal.
    /// Use this during cookie validation where context.Principal differs from HttpContext.User.
    /// </summary>
    ValueTask EstablishSessionAsync(HttpContext httpContext, ClaimsPrincipal principal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transitions from anonymous to authenticated session during sign-in.
    /// </summary>
    ValueTask<string> TransitionToAuthenticatedAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transitions from anonymous to authenticated session during sign-in.
    /// Use this overload when HttpContext is directly available.
    /// </summary>
    ValueTask<string> TransitionToAuthenticatedAsync(
        HttpContext httpContext,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the current session and deletes the session cookie.
    /// </summary>
    void ClearSession();
}
