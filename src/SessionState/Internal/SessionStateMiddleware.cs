using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SessionState.Internal.Session;

namespace SessionState.Internal;

/// <summary>
/// Middleware that establishes the session for each request.
/// Must run after UseAuthentication() and before endpoint routing.
/// </summary>
internal sealed partial class SessionStateMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionStateMiddleware> _logger;

    public SessionStateMiddleware(RequestDelegate next, ILogger<SessionStateMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ISessionStateSessionManager sessionManager)
    {
        try
        {
            await sessionManager.EstablishSessionAsync(context.RequestAborted);
        }
        catch (Exception ex)
        {
            LogSessionEstablishmentError(ex);
            // Don't fail the request - let it continue without a session
            // State operations will fail gracefully later if needed
        }

        try
        {
            await _next(context);
        }
        finally
        {
            // Clear async local to prevent any potential leakage to subsequent requests
            SessionStateAsyncLocal.Clear();
        }
    }

    [LoggerMessage(LogLevel.Error, "Failed to establish session")]
    partial void LogSessionEstablishmentError(Exception ex);
}
