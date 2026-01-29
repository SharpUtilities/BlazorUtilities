using System.Threading.RateLimiting;
using BlazorState.Core;
using BlazorState.Core.Builder;
using BlazorState.Core.KeepAlive;
using BlazorState.Core.Options;
using BlazorState.Internal.Backends;
using BlazorState.Internal.Circuits;
using BlazorState.Internal.Cookies;
using BlazorState.Internal.Events;
using BlazorState.Internal.Http;
using BlazorState.Internal.Keys;
using BlazorState.Internal.Services;
using BlazorState.Internal.Session;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace BlazorState.Internal;

/// <summary>
/// Default implementation of ISessionStateBuilder.
/// </summary>
internal sealed class BlazorStateBuilder : IBlazorStateBuilder
{
    internal const string RateLimiterPolicyName = "BlazorStateKeepAlive";

    private bool _backendRegistered;
    private bool _keepAliveEnabled;
    private bool _coreServicesRegistered;

    public BlazorStateBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; }

    public IBlazorStateBuilder WithInMemoryBackend()
    {
        EnsureBackendNotRegistered();
        EnsureCoreServicesRegistered();
        Services.AddSingleton<IBlazorStateBackend, InMemoryBlazorStateBackend>();
        _backendRegistered = true;
        return this;
    }

    public IBlazorStateBuilder WithBackend<TBackend>()
        where TBackend : class, IBlazorStateBackend
    {
        EnsureBackendNotRegistered();
        EnsureCoreServicesRegistered();
        Services.AddSingleton<IBlazorStateBackend, TBackend>();
        _backendRegistered = true;
        return this;
    }

    public IBlazorStateBuilder WithBackend<TBackend>(Func<IServiceProvider, TBackend> factory)
        where TBackend : class, IBlazorStateBackend
    {
        EnsureBackendNotRegistered();
        EnsureCoreServicesRegistered();
        Services.AddSingleton<IBlazorStateBackend>(factory);
        _backendRegistered = true;
        return this;
    }

    public IBlazorStateBuilder WithKeepAlive(Action<BlazorStateKeepAliveOptions>? configure = null)
    {
        if (_keepAliveEnabled)
        {
            throw new InvalidOperationException("Keep-alive has already been configured.");
        }

        EnsureCoreServicesRegistered();

        if (configure is not null)
        {
            Services.Configure(configure);
        }

        Services.TryAddSingleton<IBlazorStateKeepAliveService, BlazorStateKeepAliveService>();

        Services.AddRateLimiter(limiterOptions =>
        {
            limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiterOptions.AddPolicy(RateLimiterPolicyName, context =>
            {
                var keepAliveOptions = context.RequestServices
                    .GetRequiredService<IOptions<BlazorStateKeepAliveOptions>>().Value;

                var sessionOptions = context.RequestServices
                    .GetRequiredService<IOptions<BlazorStateOptions>>().Value;

                string? partitionKey = null;

                if (context.Request.Cookies.TryGetValue(sessionOptions.CookieName, out var cookieValue)
                    && !string.IsNullOrEmpty(cookieValue))
                {
                    partitionKey = cookieValue;
                }

                if (partitionKey is null)
                {
                    var claimValue = context.User.FindFirst(sessionOptions.ClaimType)?.Value;
                    if (!string.IsNullOrEmpty(claimValue))
                    {
                        partitionKey = claimValue;
                    }
                }

                if (partitionKey is null)
                {
                    var remoteIp = context.Connection.RemoteIpAddress;
                    if (remoteIp is not null)
                    {
                        partitionKey = remoteIp.ToString();
                    }
                }

                if (partitionKey is null)
                {
                    return RateLimitPartition.GetSlidingWindowLimiter("__global__", _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 1,
                        Window = keepAliveOptions.CheckInterval,
                        SegmentsPerWindow = 1,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
                }

                return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = keepAliveOptions.RateLimitPermitLimit,
                    Window = keepAliveOptions.CheckInterval,
                    SegmentsPerWindow = 2,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
            });
        });

        _keepAliveEnabled = true;
        return this;
    }

    internal IBlazorStateBuilder ConfigureEvents(Action<BlazorStateEventOptions> configure)
    {
        EnsureCoreServicesRegistered();
        Services.Configure(configure);
        return this;
    }

    public IBlazorStateBuilder ConfigureOptions(Action<BlazorStateOptions> configure)
    {
        EnsureCoreServicesRegistered();
        Services.Configure(configure);
        return this;
    }

    internal bool IsBackendRegistered => _backendRegistered;
    internal bool IsKeepAliveEnabled => _keepAliveEnabled;

    private void EnsureBackendNotRegistered()
    {
        if (_backendRegistered)
        {
            throw new InvalidOperationException(
                "A backend has already been registered. " +
                "Only one backend can be configured.");
        }
    }

    private void EnsureCoreServicesRegistered()
    {
        if (_coreServicesRegistered)
        {
            return;
        }

        ConfigureCore();
        _coreServicesRegistered = true;
    }

    private void ConfigureCore()
    {
        Services.AddHttpContextAccessor();

        // Singletons
        Services.TryAddSingleton<BlazorStatePipelineMarker>();
        Services.TryAddSingleton<BlazorStateTimeProvider>();
        Services.TryAddSingleton<BlazorStateAsyncLocalAccessor>();
        Services.TryAddSingleton<IBlazorStateKeyProtector, BlazorStateKeyProtector>();
        Services.TryAddSingleton<IBlazorStateCookieManager, BlazorStateCookieManager>();

        // Automatically add session propagation to ALL HttpClients created via IHttpClientFactory
        Services.AddSingleton<IHttpMessageHandlerBuilderFilter, BlazorStateHttpMessageHandlerBuilderFilter>();

        // Event dispatcher (singleton)
        Services.TryAddSingleton<BlazorStateEventDispatcher>();

        // Scoped services
        Services.TryAddScoped<BlazorStateContext>();
        Services.TryAddScoped<IBlazorStateKeyAccessor, BlazorStateKeyAccessor>();
        Services.TryAddScoped<IBlazorStateSessionManager, BlazorStateSessionManager>();
        Services.TryAddScoped<IBlazorStateLockManager, BlazorStateLockManager>();

        // Circuit handler
        Services.TryAddScoped<BlazorStateCircuitHandler>();
        Services.AddScoped<CircuitHandler>(sp => sp.GetRequiredService<BlazorStateCircuitHandler>());

        // State services
        Services.TryAddScoped(typeof(BlazorStateCache<>));
        Services.TryAddScoped(typeof(BlazorState<>), typeof(DefaultBlazorState<>));

        // Background services
        Services.AddHostedService<BlazorStateCleanupService>();

        // Register builder for runtime access
        Services.AddSingleton(this);
    }
}
