using System.Security.Claims;
using BlazorState.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace BlazorState.Examples.Services;

/// <summary>
/// Demo cookie authentication events to verify that EventsType works with BlazorState.
/// </summary>
public class DemoCookieEvents : CookieAuthenticationEvents
{
    private readonly ILogger<DemoCookieEvents> _logger;

    public DemoCookieEvents(ILogger<DemoCookieEvents> logger)
    {
        _logger = logger;
    }

    public override Task SigningIn(CookieSigningInContext context)
    {
        var userName = context.Principal?.Identity?.Name ?? "Unknown";
        _logger.LogInformation("[DemoCookieEvents] SigningIn: {UserName}", userName);
        return Task.CompletedTask;
    }

    public override Task SignedIn(CookieSignedInContext context)
    {
        var userName = context.Principal?.Identity?.Name ?? "Unknown";
        _logger.LogInformation("[DemoCookieEvents] SignedIn: {UserName}", userName);
        return Task.CompletedTask;
    }

    public override Task SigningOut(CookieSigningOutContext context)
    {
        _logger.LogInformation("[DemoCookieEvents] SigningOut");
        return Task.CompletedTask;
    }

    public override Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var userName = context.Principal?.Identity?.Name ?? "Unknown";
        _logger.LogDebug("[DemoCookieEvents] ValidatePrincipal: {UserName}", userName);
        return Task.CompletedTask;
    }

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        _logger.LogInformation("[DemoCookieEvents] RedirectToLogin: {RedirectUri}", context.RedirectUri);
        return base.RedirectToLogin(context);
    }

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        _logger.LogInformation("[DemoCookieEvents] RedirectToAccessDenied: {RedirectUri}", context.RedirectUri);
        return base.RedirectToAccessDenied(context);
    }
}

/// <summary>
/// Demo key generator that shows different sharing behaviors:
/// - Bob: Shared across all sessions (uses user ID as key)
/// - Alice, Charlie, etc.: Per-session (uses unique GUID)
/// - Anonymous: Per-session (uses unique GUID)
/// </summary>
public class DemoKeyGenerator : IBlazorStateKeyGenerator
{
    public ValueTask<string> GenerateNonAuthenticatedKeyAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        // Anonymous users always get a unique session key
        return ValueTask.FromResult(Guid.NewGuid().ToString());
    }

    public ValueTask<string> GenerateAuthenticatedKeyAsync(
        BlazorStateKeyContext context,
        string? nonAuthenticatedKey,
        CancellationToken cancellationToken = default)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.Equals(userId, "bob", StringComparison.OrdinalIgnoreCase))
        {
            // Bob shares state across all his sessions
            return ValueTask.FromResult($"shared:user:{userId}");
        }

        // Everyone else gets a unique session key
        // Optionally, we could reuse the anonymous key if one existed
        return ValueTask.FromResult(nonAuthenticatedKey ?? Guid.NewGuid().ToString());
    }
}
