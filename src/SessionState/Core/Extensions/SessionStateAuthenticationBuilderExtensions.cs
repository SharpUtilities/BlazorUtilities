using SessionState.Internal.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for integrating SessionState with authentication.
/// </summary>
public static class SessionStateAuthenticationBuilderExtensions
{
    /// <summary>
    /// Adds SessionState integration to cookie authentication.
    /// Injects the session state key into the auth cookie at sign-in time,
    /// migrating any existing anonymous session data.
    /// </summary>
    /// <param name="builder">The authentication builder.</param>
    /// <param name="authenticationScheme">The cookie authentication scheme name. Defaults to "Cookies".</param>
    /// <returns>The authentication builder for chaining.</returns>
    public static AuthenticationBuilder WithBlazorSessionState(
        this AuthenticationBuilder builder,
        string authenticationScheme)
    {
        // Use PostConfigure to run after all other configurations
        builder.Services.AddOptions<CookieAuthenticationOptions>(authenticationScheme)
            .PostConfigure<IServiceProvider>((cookieOptions, services) =>
            {
                // Capture existing events configuration
                var existingEvents = cookieOptions.Events;
                var existingEventsType = cookieOptions.EventsType;

                // If EventsType is specified, ensure it's registered in DI
                if (existingEventsType is not null)
                {
                    // We can't modify DI here, but we'll resolve it at runtime
                    // The ResolveInnerEvents method handles this
                }

                // Replace with our wrapper that resolves dependencies at runtime
                cookieOptions.Events = new SessionStateCookieAuthenticationEvents(
                    existingEvents,
                    existingEventsType);

                // Clear EventsType since we handle resolution ourselves
                cookieOptions.EventsType = null;
            });

        return builder;
    }
}
