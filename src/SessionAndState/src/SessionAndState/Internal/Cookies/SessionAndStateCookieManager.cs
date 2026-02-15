using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SessionAndState.Core.Options;
using SessionAndState.Internal.Keys;

namespace SessionAndState.Internal.Cookies;

/// <summary>
/// Default implementation of <see cref="ISessionAndStateCookieManager"/>.
/// Handles encrypted cookie operations for anonymous session keys.
/// </summary>
internal sealed partial class SessionAndStateCookieManager : ISessionAndStateCookieManager
{
    private readonly ISessionAndStateKeyProtector _protector;
    private readonly AnonymousCookieSessionOptions _options;
    private readonly SessionAndStateTimeProvider _timeProvider;
    private readonly ILogger<SessionAndStateCookieManager> _logger;

    public SessionAndStateCookieManager(
        ISessionAndStateKeyProtector protector,
        IOptions<AnonymousCookieSessionOptions> options,
        SessionAndStateTimeProvider timeProvider,
        ILogger<SessionAndStateCookieManager> logger)
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

        var cookieOptions = new CookieOptions
        {
            Domain = _options.Domain,
            Path = _options.Path,
            HttpOnly = _options.HttpOnly,
            Secure = _options.Secure,
            SameSite = _options.SameSite,
            IsEssential = _options.IsEssential,
            MaxAge = _options.MaxAge,
            Expires = _options.Expires
        };

        if (cookieOptions.Expires is null && cookieOptions.MaxAge is not null)
        {
            cookieOptions.Expires = _timeProvider.GetUtcNow().Add(cookieOptions.MaxAge.Value);
        }

        context.Response.Cookies.Append(_options.CookieName, protectedValue, cookieOptions);

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
