using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using SessionState.Core;
using SessionState.Core.Builder;
using SessionState.Core.Events;
using SessionState.Core.KeepAlive;
using SessionState.Core.Options;
using SessionState.Internal.Backends;
using SessionState.Internal.Circuits;
using SessionState.Internal.Cookies;
using SessionState.Internal.Events;
using SessionState.Internal.Http;
using SessionState.Internal.Keys;
using SessionState.Internal.Services;
using SessionState.Internal.Session;

namespace SessionState.Internal;

/// <summary>
/// Default implementation of ISessionStateBuilder.
/// </summary>
internal sealed class SessionStateBuilder : ISessionStateBuilder
{
    internal const string RateLimiterPolicyName = "BlazorSessionStateKeepAlive";

    private bool _backendRegistered;
    private bool _keepAliveEnabled;
    private bool _coreServicesRegistered;

    public SessionStateBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; }

    public ISessionStateBuilder WithInMemoryBackend()
    {
        EnsureBackendNotRegistered();
        EnsureCoreServicesRegistered();
        Services.AddSingleton<ISessionStateBackend, InMemorySessionStateBackend>();
        _backendRegistered = true;
        return this;
    }

    public ISessionStateBuilder WithBackend<TBackend>()
        where TBackend : class, ISessionStateBackend
    {
        EnsureBackendNotRegistered();
        EnsureCoreServicesRegistered();
        Services.AddSingleton<ISessionStateBackend, TBackend>();
        _backendRegistered = true;
        return this;
    }

    public ISessionStateBuilder WithBackend<TBackend>(Func<IServiceProvider, TBackend> factory)
        where TBackend : class, ISessionStateBackend
    {
        EnsureBackendNotRegistered();
        EnsureCoreServicesRegistered();
        Services.AddSingleton<ISessionStateBackend>(factory);
        _backendRegistered = true;
        return this;
    }

    public ISessionStateBuilder WithKeepAlive(Action<SessionStateKeepAliveOptions>? configure = null)
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

        Services.TryAddSingleton<ISessionStateKeepAliveService, SessionStateKeepAliveService>();

        Services.AddRateLimiter(limiterOptions =>
        {
            limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiterOptions.AddPolicy(RateLimiterPolicyName, context =>
            {
                var keepAliveOptions = context.RequestServices
                    .GetRequiredService<IOptions<SessionStateKeepAliveOptions>>().Value;

                var sessionOptions = context.RequestServices
                    .GetRequiredService<IOptions<SessionStateOptions>>().Value;

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

    public ISessionStateBuilder ConfigureEvents(Action<SessionStateEventOptions> configure)
    {
        EnsureCoreServicesRegistered();
        Services.Configure(configure);
        return this;
    }

    public ISessionStateBuilder ConfigureOptions(Action<SessionStateOptions> configure)
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
        Services.TryAddSingleton<SessionStatePipelineMarker>();
        Services.TryAddSingleton<SessionStateTimeProvider>();
        Services.TryAddSingleton<ISessionStateKeyProtector, SessionStateKeyProtector>();
        Services.TryAddSingleton<ISessionStateCookieManager, SessionStateCookieManager>();

        // Automatically add session propagation to ALL HttpClients created via IHttpClientFactory
        Services.AddSingleton<IHttpMessageHandlerBuilderFilter, SessionStateHttpMessageHandlerBuilderFilter>();

        // Event dispatcher (singleton)
        Services.TryAddSingleton<SessionStateEventDispatcher>();
        Services.TryAddSingleton<ISessionStateEventDispatcher>(sp =>
            sp.GetRequiredService<SessionStateEventDispatcher>());

        // Scoped services
        Services.TryAddScoped<SessionStateContext>();
        Services.TryAddScoped<ISessionStateKeyAccessor, SessionStateKeyAccessor>();
        Services.TryAddScoped<ISessionStateSessionManager, SessionStateSessionManager>();
        Services.TryAddScoped<ISessionStateLockManager, SessionStateLockManager>();

        // Circuit handler
        Services.TryAddScoped<SessionStateCircuitHandler>();
        Services.AddScoped<CircuitHandler>(sp => sp.GetRequiredService<SessionStateCircuitHandler>());

        // State services
        Services.TryAddScoped(typeof(SessionStateCache<>));
        Services.TryAddScoped(typeof(SessionState<>), typeof(DefaultSessionState<>));

        // Background services
        Services.AddHostedService<SessionStateCleanupService>();

        // Register builder for runtime access
        Services.AddSingleton(this);
    }
}
