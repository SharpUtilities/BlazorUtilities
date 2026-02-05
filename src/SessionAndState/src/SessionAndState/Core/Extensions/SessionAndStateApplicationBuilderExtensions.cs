using SessionAndState.Core.KeepAlive;
using SessionAndState.Internal;
using SessionAndState.Internal.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring SessionAndState endpoints.
/// </summary>
public static class SessionAndStateApplicationBuilderExtensions
{
    /// <summary>
    /// Adds SessionAndState middleware and endpoints to the application pipeline.
    /// Call this after UseRouting() and UseAuthentication()/UseAuthorization().
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when Data Protection has not been configured. Call <c>services.AddDataProtection()</c>
    /// during service registration.
    /// </exception>
    public static IApplicationBuilder UseSessionAndState<TApplication>(this TApplication app)
    where TApplication : class, IHost, IApplicationBuilder, IEndpointRouteBuilder
    {
        // Verify Data Protection is configured
        var dataProtectionProvider = app.ApplicationServices.GetService<IDataProtectionProvider>();
        if (dataProtectionProvider is null)
        {
            throw new InvalidOperationException(
                "Data Protection has not been configured. " +
                "SessionAndState requires Data Protection to secure anonymous session cookies. " +
                "Call 'services.AddDataProtection()' during service registration.");
        }

        // Mark as configured
        var marker = app.ApplicationServices.GetRequiredService<SessionAndStatePipelineMarker>();
        marker.MarkAsConfigured();

        // Wire up events
        SessionStateEventWiringHelper.WireUpEvents(app.ApplicationServices);

        var featureFlags = app.ApplicationServices.GetRequiredService<IOptions<SessionAndStateFeatureFlags>>().Value;
        if (featureFlags.KeepAliveEnabled)
        {
            app.UseRateLimiter();
        }

        // Add middleware to ensure session key is established before response starts (for anonymous users)
        app.UseMiddleware<SessionAndStateMiddleware>();

        // If this is a WebApplication (minimal API), we can map endpoints directly
        MapKeepAliveEndpoint(app);

        return app;
    }

    private static void MapKeepAliveEndpoint(IEndpointRouteBuilder endpoints)
    {
        var builder = endpoints.ServiceProvider.GetService<SessionStateBuilder>();

        if (builder?.IsKeepAliveEnabled != true)
        {
            return;
        }

        var options = endpoints.ServiceProvider
            .GetRequiredService<IOptions<SessionAndStateKeepAliveOptions>>().Value;

        var endpointBuilder = endpoints.MapGet(options.Endpoint, context =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        });

        endpointBuilder.RequireRateLimiting(SessionStateBuilder.RateLimiterPolicyName);

        if (options.RequireAuthentication)
        {
            endpointBuilder.RequireAuthorization();
        }
    }
}
