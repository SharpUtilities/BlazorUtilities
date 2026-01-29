using System.Security.Claims;
using BlazorState.Core;
using BlazorState.Core.Options;
using BlazorState.Internal.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlazorState.Internal.Session;

/// <summary>
/// Default implementation of <see cref="IBlazorStateSessionManager"/>.
/// Coordinates session establishment, transitions, and cleanup.
/// </summary>
internal sealed partial class BlazorStateSessionManager : IBlazorStateSessionManager, IDisposable
{
    private readonly BlazorStateContext _context;
    private readonly IBlazorStateCookieManager _cookieManager;
    private readonly IBlazorStateKeyGenerator _keyGenerator;
    private readonly IBlazorStateBackend _backend;
    private readonly BlazorStateOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<BlazorStateSessionManager> _logger;

    private readonly SemaphoreSlim _establishLock = new(1, 1);
    private bool _disposed;

    public BlazorStateSessionManager(
        BlazorStateContext context,
        IBlazorStateCookieManager cookieManager,
        IBlazorStateKeyGenerator keyGenerator,
        IBlazorStateBackend backend,
        IOptions<BlazorStateOptions> options,
        IHttpContextAccessor httpContextAccessor,
        ILogger<BlazorStateSessionManager> logger)
    {
        _context = context;
        _cookieManager = cookieManager;
        _keyGenerator = keyGenerator;
        _backend = backend;
        _options = options.Value;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public ValueTask EstablishSessionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is null)
        {
            LogNoHttpContext();
            _context.SetNoSession();
            return ValueTask.CompletedTask;
        }

        return EstablishSessionAsync(httpContext, httpContext.User, cancellationToken);
    }

    public ValueTask EstablishSessionAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return EstablishSessionAsync(httpContext, httpContext.User, cancellationToken);
    }

    public async ValueTask EstablishSessionAsync(HttpContext httpContext, ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Already established this request/scope?
        if (_context.IsEstablished)
        {
            return;
        }

        await _establishLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            if (_context.IsEstablished)
            {
                return;
            }

            // Check for authenticated user first
            if (principal.Identity?.IsAuthenticated == true)
            {
                EstablishAuthenticatedSession(httpContext, principal);
                return;
            }

            // Anonymous user - check if allowed
            if (_options.RequireAuthentication)
            {
                LogSkippedAnonymousSession();
                _context.SetNoSession();
                return;
            }

            // Try to read existing anonymous session from cookie
            var existingKey = _cookieManager.ReadSessionKey(httpContext);
            if (existingKey is not null)
            {
                _context.Set(existingKey, isAuthenticated: false);
                LogEstablishedFromCookie(FormatKey(existingKey));
                return;
            }

            // Can we create a new session? Only if response hasn't started
            if (httpContext.Response.HasStarted)
            {
                LogCannotCreateSessionResponseStarted();
                _context.SetNoSession();
                return;
            }

            // Create new anonymous session
            var newKey = await _keyGenerator.GenerateNonAuthenticatedKeyAsync(httpContext, cancellationToken);
            ValidateKey(newKey);

            _cookieManager.WriteSessionKey(httpContext, newKey);
            _context.Set(newKey, isAuthenticated: false);
            LogCreatedAnonymousSession(FormatKey(newKey));
        }
        finally
        {
            _establishLock.Release();
        }
    }

    public ValueTask<string> TransitionToAuthenticatedAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException(
                "Cannot transition session without HTTP context. " +
                "Use the overload that accepts HttpContext directly.");

        return TransitionToAuthenticatedAsync(httpContext, principal, cancellationToken);
    }

    public async ValueTask<string> TransitionToAuthenticatedAsync(
        HttpContext httpContext,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Check if already has a session key claim
        var existingClaimKey = principal.FindFirstValue(_options.ClaimType);
        if (!string.IsNullOrEmpty(existingClaimKey))
        {
            _context.Set(existingClaimKey, isAuthenticated: true);
            return existingClaimKey;
        }

        // Get any existing anonymous session key
        string? anonymousKey = null;
        var (currentKey, isAuthenticated, isEstablished) = _context.GetState();

        if (isEstablished && !isAuthenticated && currentKey is not null)
        {
            anonymousKey = currentKey;
        }
        else
        {
            // Context might not be established yet, try reading from cookie
            anonymousKey = _cookieManager.ReadSessionKey(httpContext);
        }

        // Generate new authenticated key
        var keyContext = new BlazorStateKeyContext
        {
            User = principal,
            HttpContext = httpContext,
            Services = httpContext.RequestServices
        };

        var newKey = await _keyGenerator.GenerateAuthenticatedKeyAsync(keyContext, anonymousKey, cancellationToken);
        ValidateKey(newKey);

        // Migrate data if we had an anonymous session
        if (!string.IsNullOrWhiteSpace(anonymousKey) && anonymousKey != newKey)
        {
            await MigrateSessionDataAsync(anonymousKey, newKey, cancellationToken);
        }

        // Delete the anonymous cookie
        _cookieManager.DeleteSessionKey(httpContext);

        // Update context
        _context.Set(newKey, isAuthenticated: true);

        LogTransitionedToAuthenticated(FormatKey(newKey));
        return newKey;
    }

    public void ClearSession()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is not null)
        {
            _cookieManager.DeleteSessionKey(httpContext);
        }

        _context.Clear();
        LogSessionCleared();
    }

    private void EstablishAuthenticatedSession(HttpContext httpContext, ClaimsPrincipal principal)
    {
        var claimKey = principal.FindFirstValue(_options.ClaimType);

        if (!string.IsNullOrEmpty(claimKey))
        {
            _context.Set(claimKey, isAuthenticated: true);
            LogEstablishedFromClaim(FormatKey(claimKey));
        }
        else
        {
            LogMissingSessionKeyClaim();
            _context.SetNoSession();
        }
    }

    private async Task MigrateSessionDataAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken)
    {
        try
        {
            await _backend.MigrateSessionAsync(sourceKey, destinationKey, cancellationToken);
            LogSessionMigrated(FormatKey(sourceKey), FormatKey(destinationKey));
        }
        catch (Exception ex)
        {
            LogMigrationFailed(FormatKey(sourceKey), FormatKey(destinationKey), ex);
        }
    }

    private static void ValidateKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Key generator returned null or empty key.");
        }
    }

    private static string FormatKey(string key) =>
        key.Length > 8 ? key[..8] + "..." : key;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _establishLock.Dispose();
    }

    [LoggerMessage(LogLevel.Debug, "Established session from claim: {KeyPrefix}")]
    partial void LogEstablishedFromClaim(string keyPrefix);

    [LoggerMessage(LogLevel.Debug, "Established session from cookie: {KeyPrefix}")]
    partial void LogEstablishedFromCookie(string keyPrefix);

    [LoggerMessage(LogLevel.Debug, "Created new anonymous session: {KeyPrefix}")]
    partial void LogCreatedAnonymousSession(string keyPrefix);

    [LoggerMessage(LogLevel.Debug, "Skipped anonymous session - RequireAuthentication is enabled")]
    partial void LogSkippedAnonymousSession();

    [LoggerMessage(LogLevel.Debug, "Transitioned to authenticated session: {KeyPrefix}")]
    partial void LogTransitionedToAuthenticated(string keyPrefix);

    [LoggerMessage(LogLevel.Debug, "Migrated session from {OldKeyPrefix} to {NewKeyPrefix}")]
    partial void LogSessionMigrated(string oldKeyPrefix, string newKeyPrefix);

    [LoggerMessage(LogLevel.Warning, "Failed to migrate session from {OldKeyPrefix} to {NewKeyPrefix}. Anonymous session data was lost.")]
    partial void LogMigrationFailed(string oldKeyPrefix, string newKeyPrefix, Exception ex);

    [LoggerMessage(LogLevel.Warning, "Authenticated user has no session key claim. Was WithBlazorSessionState() called on the authentication builder?")]
    partial void LogMissingSessionKeyClaim();

    [LoggerMessage(LogLevel.Debug, "Session cleared")]
    partial void LogSessionCleared();

    [LoggerMessage(LogLevel.Warning, "No HTTP context available. Session cannot be established in this scope.")]
    partial void LogNoHttpContext();

    [LoggerMessage(LogLevel.Warning, "Cannot create new anonymous session - HTTP response has already started.")]
    partial void LogCannotCreateSessionResponseStarted();
}
