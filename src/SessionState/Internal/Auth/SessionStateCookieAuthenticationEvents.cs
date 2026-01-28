using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SessionState.Core.Options;
using SessionState.Internal.Session;

namespace SessionState.Internal.Auth;

/// <summary>
/// Cookie authentication events that integrate with SessionState.
/// Handles session key injection during sign-in.
/// Wraps any existing events configured by the application.
/// </summary>
internal sealed class SessionStateCookieAuthenticationEvents : CookieAuthenticationEvents
{
    private readonly CookieAuthenticationEvents? _innerEvents;
    private readonly Type? _innerEventsType;

    public SessionStateCookieAuthenticationEvents(
        CookieAuthenticationEvents? innerEvents,
        Type? innerEventsType)
    {
        if (innerEvents?.GetType() == typeof(CookieAuthenticationEvents) && innerEventsType is not null)
        {
            _innerEvents = null;
        }
        else
        {
            _innerEvents = innerEvents;
        }

        _innerEventsType = innerEventsType;
    }

    private CookieAuthenticationEvents? ResolveInnerEvents(IServiceProvider services)
    {
        if (_innerEventsType is not null)
        {
            if (services.GetService(_innerEventsType) is CookieAuthenticationEvents resolved)
            {
                return resolved;
            }
        }

        if (_innerEvents is not null && _innerEvents.GetType() != typeof(CookieAuthenticationEvents))
        {
            return _innerEvents;
        }

        return _innerEvents;
    }

    public override async Task SigningIn(CookieSigningInContext context)
    {
        await InjectSessionKeyClaimAsync(context);

        var innerEvents = ResolveInnerEvents(context.HttpContext.RequestServices);
        if (innerEvents is not null)
        {
            await innerEvents.SigningIn(context);
        }
    }

    private async Task InjectSessionKeyClaimAsync(CookieSigningInContext context)
    {
        var services = context.HttpContext.RequestServices;
        var options = services.GetRequiredService<IOptions<SessionStateOptions>>().Value;

        if (context.Principal?.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        if (identity.HasClaim(c => c.Type == options.ClaimType))
        {
            return;
        }

        var sessionManager = services.GetRequiredService<ISessionStateSessionManager>();

        var newKey = await sessionManager.TransitionToAuthenticatedAsync(
            context.HttpContext,
            context.Principal,
            context.HttpContext.RequestAborted);

        identity.AddClaim(new Claim(options.ClaimType, newKey));
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        // Establish session early so it's available during validation
        // Must use context.Principal (not HttpContext.User) as that's the principal being validated
        if (context.Principal is not null)
        {
            var sessionManager = context.HttpContext.RequestServices
                .GetRequiredService<ISessionStateSessionManager>();

            await sessionManager.EstablishSessionAsync(
                context.HttpContext,
                context.Principal,
                context.HttpContext.RequestAborted);
        }

        var innerEvents = ResolveInnerEvents(context.HttpContext.RequestServices);
        if (innerEvents is not null)
        {
            await innerEvents.ValidatePrincipal(context);
        }
    }

    public override Task SignedIn(CookieSignedInContext context)
    {
        var innerEvents = ResolveInnerEvents(context.HttpContext.RequestServices);
        return innerEvents?.SignedIn(context) ?? Task.CompletedTask;
    }

    public override Task SigningOut(CookieSigningOutContext context)
    {
        var innerEvents = ResolveInnerEvents(context.HttpContext.RequestServices);
        return innerEvents?.SigningOut(context) ?? Task.CompletedTask;
    }

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        var innerEvents = ResolveInnerEvents(context.HttpContext.RequestServices);
        return innerEvents?.RedirectToLogin(context) ?? base.RedirectToLogin(context);
    }

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
    {
        var innerEvents = ResolveInnerEvents(context.HttpContext.RequestServices);
        return innerEvents?.RedirectToAccessDenied(context) ?? base.RedirectToAccessDenied(context);
    }

    public override Task RedirectToLogout(RedirectContext<CookieAuthenticationOptions> context)
    {
        var innerEvents = ResolveInnerEvents(context.HttpContext.RequestServices);
        return innerEvents?.RedirectToLogout(context) ?? base.RedirectToLogout(context);
    }

    public override Task RedirectToReturnUrl(RedirectContext<CookieAuthenticationOptions> context)
    {
        var innerEvents = ResolveInnerEvents(context.HttpContext.RequestServices);
        return innerEvents?.RedirectToReturnUrl(context) ?? base.RedirectToReturnUrl(context);
    }
}
