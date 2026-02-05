using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SessionAndState.Core;
using SessionAndState.Core.Options;

namespace SessionAndState.Examples.Services;

public sealed class TokenState : SessionStateTypeBase<TokenState>, ISessionStateType<TokenState>
{
    public static Expiration SlidingExpiration => Expiration.AfterMinutes(5);
    public static Expiration AbsoluteExpiration => Expiration.AfterHours(1);

    public required string Token
    {
        get => field;
        set
        {
            SetField(ref field, value);
        }
    }

    public override bool Equals(TokenState? other)
    {
        if (other is null)
        {
            return false;
        }

        return Token == other.Token;
    }

    // ReSharper disable once NonReadonlyMemberInGetHashCode
    public override int GetHashCode() => Token.GetHashCode();
}


/// <summary>
/// Demo cookie authentication events to verify that EventsType works with BlazorState.
/// </summary>
public sealed class DemoCookieEvents : CookieAuthenticationEvents
{
    private readonly ILogger<DemoCookieEvents> _logger;
    private readonly SessionState<TokenState> _tokenState;

    public DemoCookieEvents(ILogger<DemoCookieEvents> logger, SessionState<TokenState> tokenState)
    {
        _logger = logger;
        _tokenState = tokenState;
    }

    public override Task SigningIn(CookieSigningInContext context)
    {
        var userName = context.Principal?.Identity?.Name ?? "Unknown";
        _logger.LogInformation("[DemoCookieEvents] SigningIn: {UserName}", userName);
        return Task.CompletedTask;
    }

    public override async Task SignedIn(CookieSignedInContext context)
    {
        var userName = context.Principal?.Identity?.Name ?? "Unknown";
        var jwt = context.Principal?.FindFirstValue("jwt");
        await _tokenState.SetAsync(new TokenState
        {
            Token = string.IsNullOrWhiteSpace(jwt) ? string.Empty : jwt,
        });

        _logger.LogInformation("[DemoCookieEvents] SignedIn: {UserName}", userName);
    }

    public override Task SigningOut(CookieSigningOutContext context)
    {
        _logger.LogInformation("[DemoCookieEvents] SigningOut");
        return Task.CompletedTask;
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var userName = context.Principal?.Identity?.Name ?? "Unknown";
        if (context.Principal?.Identity?.IsAuthenticated == true && !_tokenState.TryGetValue(out _))
        {
            var jwt = context.Principal?.FindFirstValue("jwt");
            await _tokenState.SetAsync(new TokenState
            {
                Token = string.IsNullOrWhiteSpace(jwt) ? string.Empty : jwt,
            });
        }

        _logger.LogDebug("[DemoCookieEvents] ValidatePrincipal: {UserName}", userName);
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
