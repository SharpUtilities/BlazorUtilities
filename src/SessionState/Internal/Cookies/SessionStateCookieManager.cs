using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SessionState.Core.Options;
using SessionState.Internal.Keys;

namespace SessionState.Internal.Cookies;

/// <summary>
/// Default implementation of <see cref="ISessionStateCookieManager"/>.
/// Handles encrypted cookie operations for anonymous session keys.
/// </summary>
internal sealed partial class SessionStateCookieManager : ISessionStateCookieManager
{
    private readonly ISessionStateKeyProtector _protector;
    private readonly SessionStateOptions _options;
    private readonly SessionStateTimeProvider _timeProvider;
    private readonly ILogger<SessionStateCookieManager> _logger;

    public SessionStateCookieManager(
        ISessionStateKeyProtector protector,
        IOptions<SessionStateOptions> options,
        SessionStateTimeProvider timeProvider,
        ILogger<SessionStateCookieManager> logger)
    {
        _protector = protector;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public string? ReadSessionKey(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(_options.CookieName, out var cookieValue))
        {
            return null;
        }

        var decryptedKey = _protector.Unprotect(cookieValue);
        if (!string.IsNullOrWhiteSpace(decryptedKey))
        {
            LogReadFromCookie();
            return decryptedKey;
        }

        LogInvalidCookieDetected();

        // Clean up invalid cookie if response hasn't started
        if (!context.Response.HasStarted)
        {
            context.Response.Cookies.Delete(_options.CookieName);
        }

        return null;
    }

    public void WriteSessionKey(HttpContext context, string key)
    {
        if (context.Response.HasStarted)
        {
            LogCannotWriteCookieResponseStarted();
            return;
        }

        var protectedValue = _protector.Protect(key);
        context.Response.Cookies.Append(_options.CookieName, protectedValue, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            IsEssential = true,
            Expires = _options.CookieExpiration.Expires
                ? _timeProvider.GetUtcNow().Add(_options.CookieExpiration.Duration)
                : null,
            MaxAge = _options.CookieMaxAge,
        });

        LogWroteCookie();
    }

    public void DeleteSessionKey(HttpContext context)
    {
        if (context.Response.HasStarted)
        {
            LogCannotDeleteCookieResponseStarted();
            return;
        }

        context.Response.Cookies.Delete(_options.CookieName);
        LogDeletedCookie();
    }

    [LoggerMessage(LogLevel.Debug, "Read session key from encrypted cookie")]
    partial void LogReadFromCookie();

    [LoggerMessage(LogLevel.Warning, "Invalid session state cookie detected, clearing")]
    partial void LogInvalidCookieDetected();

    [LoggerMessage(LogLevel.Debug, "Wrote session key to encrypted cookie")]
    partial void LogWroteCookie();

    [LoggerMessage(LogLevel.Debug, "Deleted session cookie")]
    partial void LogDeletedCookie();

    [LoggerMessage(LogLevel.Warning, "Cannot write session cookie - HTTP response has already started")]
    partial void LogCannotWriteCookieResponseStarted();

    [LoggerMessage(LogLevel.Warning, "Cannot delete session cookie - HTTP response has already started")]
    partial void LogCannotDeleteCookieResponseStarted();
}
