using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SessionAndState.Internal.Session;

namespace SessionAndState.Internal;

/// <summary>
/// Middleware that establishes the session for each request.
/// Must run after UseAuthentication() and before endpoint routing.
/// </summary>
internal sealed partial class SessionAndStateMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionAndStateMiddleware> _logger;

    public SessionAndStateMiddleware(RequestDelegate next, ILogger<SessionAndStateMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ISessionAndStateSessionManager sessionAndStateSessionManager,
        SessionAndStateAsyncLocalAccessor asyncLocal)
    {
        try
        {
            await sessionAndStateSessionManager.EstablishSessionAsync(context.RequestAborted);
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
            asyncLocal.Clear();
        }
    }

    [LoggerMessage(LogLevel.Error, "Failed to establish session")]
    partial void LogSessionEstablishmentError(Exception ex);
}
