using BlazorState.Internal.Session;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BlazorState.Internal;

/// <summary>
/// Middleware that establishes the session for each request.
/// Must run after UseAuthentication() and before endpoint routing.
/// </summary>
internal sealed partial class BlazorStateMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<BlazorStateMiddleware> _logger;

    public BlazorStateMiddleware(RequestDelegate next, ILogger<BlazorStateMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IBlazorStateSessionManager blazorStateSessionManager,
        BlazorStateAsyncLocalAccessor asyncLocal)
    {
        try
        {
            await blazorStateSessionManager.EstablishSessionAsync(context.RequestAborted);
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
