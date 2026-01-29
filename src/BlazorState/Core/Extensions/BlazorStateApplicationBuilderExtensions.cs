using BlazorState.Core.KeepAlive;
using BlazorState.Internal;
using BlazorState.Internal.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring BlazorState endpoints.
/// </summary>
public static class BlazorStateApplicationBuilderExtensions
{
    /// <summary>
    /// Adds BlazorState middleware and endpoints to the application pipeline.
    /// Call this after UseRouting() and UseAuthentication()/UseAuthorization().
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when Data Protection has not been configured. Call <c>services.AddDataProtection()</c>
    /// during service registration.
    /// </exception>
    public static IApplicationBuilder UseBlazorState<TApplication>(this TApplication app)
    where TApplication : class, IHost, IApplicationBuilder, IEndpointRouteBuilder
    {
        // Verify Data Protection is configured
        var dataProtectionProvider = app.ApplicationServices.GetService<IDataProtectionProvider>();
        if (dataProtectionProvider is null)
        {
            throw new InvalidOperationException(
                "Data Protection has not been configured. " +
                "BlazorState requires Data Protection to secure anonymous session cookies. " +
                "Call 'services.AddDataProtection()' during service registration.");
        }

        // Mark as configured
        var marker = app.ApplicationServices.GetRequiredService<BlazorStatePipelineMarker>();
        marker.MarkAsConfigured();

        // Wire up events
        BlazorStateEventWiringHelper.WireUpEvents(app.ApplicationServices);

        // Add middleware to ensure session key is established before response starts (for anonymous users)
        app.UseMiddleware<BlazorStateMiddleware>();

        // If this is a WebApplication (minimal API), we can map endpoints directly
        MapKeepAliveEndpoint(app);

        return app;
    }

    private static void MapKeepAliveEndpoint(IEndpointRouteBuilder endpoints)
    {
        var builder = endpoints.ServiceProvider.GetService<BlazorStateBuilder>();

        if (builder?.IsKeepAliveEnabled != true)
        {
            return;
        }

        var options = endpoints.ServiceProvider
            .GetRequiredService<IOptions<BlazorStateKeepAliveOptions>>().Value;

        var endpointBuilder = endpoints.MapGet(options.Endpoint, context =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        });

        endpointBuilder.RequireRateLimiting(BlazorStateBuilder.RateLimiterPolicyName);

        if (options.RequireAuthentication)
        {
            endpointBuilder.RequireAuthorization();
        }
    }
}
