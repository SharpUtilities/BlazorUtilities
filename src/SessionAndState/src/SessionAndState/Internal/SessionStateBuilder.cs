using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SessionAndState.Core;
using SessionAndState.Core.Builder;
using SessionAndState.Core.KeepAlive;
using SessionAndState.Core.Options;
using SessionAndState.Internal.Backends;
using SessionAndState.Internal.Circuits;
using SessionAndState.Internal.Cookies;
using SessionAndState.Internal.Events;
using SessionAndState.Internal.Http;
using SessionAndState.Internal.Keys;
using SessionAndState.Internal.Services;
using SessionAndState.Internal.Session;

namespace SessionAndState.Internal;

/// <summary>
/// Default implementation of ISessionStateBuilder.
/// </summary>
internal sealed class SessionStateBuilder : ISessionStateBuilder
{
    internal const string RateLimiterPolicyName = "BlazorStateKeepAlive";

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
        Services.AddSingleton<ISessionAndStateBackend, InMemorySessionAndStateBackend>();
        _backendRegistered = true;
        return this;
    }

    public ISessionStateBuilder WithBackend<TBackend>()
        where TBackend : class, ISessionAndStateBackend
    {
        EnsureBackendNotRegistered();
        EnsureCoreServicesRegistered();
        Services.AddSingleton<ISessionAndStateBackend, TBackend>();
        _backendRegistered = true;
        return this;
    }

    public ISessionStateBuilder WithBackend<TBackend>(Func<IServiceProvider, TBackend> factory)
        where TBackend : class, ISessionAndStateBackend
    {
        EnsureBackendNotRegistered();
        EnsureCoreServicesRegistered();
        Services.AddSingleton<ISessionAndStateBackend>(factory);
        _backendRegistered = true;
        return this;
    }

    public ISessionStateBuilder WithKeepAlive(Action<SessionAndStateKeepAliveOptions>? configure = null)
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

        Services.Configure<SessionAndStateFeatureFlags>(f => f.KeepAliveEnabled = true);


        Services.AddRateLimiter(limiterOptions =>
        {
            limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiterOptions.AddPolicy(RateLimiterPolicyName, context =>
            {
                var keepAliveOptions = context.RequestServices
                    .GetRequiredService<IOptions<SessionAndStateKeepAliveOptions>>().Value;

                var sessionOptions = context.RequestServices
                    .GetRequiredService<IOptions<SessionAndStateOptions>>().Value;

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

                // We could have X-Forwarded-For, but we might want some extra validation/Logic, or pass a func to the caller to configure
                if (partitionKey is null)
                {
                    var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(forwardedFor))
                    {
                        // Take the first IP (client IP) from the comma-separated list
                        partitionKey = forwardedFor.Split(',')[0].Trim();
                    }
                }

                if (partitionKey is null)
                {
                    var logger = context.RequestServices.GetRequiredService<ILogger<SessionStateBuilder>>();
                    logger.LogWarning(
                        "Rate limiter could not determine partition key for request to {Path}",
                        context.Request.Path);

                    return keepAliveOptions.UnknownPartitionBehavior switch
                    {
                        UnknownPartitionBehavior.Allow => RateLimitPartition.GetNoLimiter("__unknown__"),
                        UnknownPartitionBehavior.Reject => RateLimitPartition.GetFixedWindowLimiter(
                            "__rejected__",
                            _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 0,
                                Window = TimeSpan.FromSeconds(1),
                                QueueLimit = 0
                            }),
                        _ => throw new InvalidOperationException(
                            $"Unknown partition behavior: {keepAliveOptions.UnknownPartitionBehavior}")
                    };
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

    internal ISessionStateBuilder ConfigureEvents(Action<SessionStateEventOptions> configure)
    {
        EnsureCoreServicesRegistered();
        Services.Configure(configure);
        return this;
    }

    public ISessionStateBuilder ConfigureOptions(Action<SessionAndStateOptions> configure)
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

        Services.Configure<SessionAndStateFeatureFlags>(f => f.KeepAliveEnabled = false);

        // Singletons
        Services.TryAddSingleton<SessionAndStatePipelineMarker>();
        Services.TryAddSingleton<SessionAndStateTimeProvider>();
        Services.TryAddSingleton<SessionAndStateAsyncLocalAccessor>();
        Services.TryAddSingleton<ISessionAndStateKeyProtector, SessionAndStateKeyProtector>();
        Services.TryAddSingleton<ISessionAndStateCookieManager, SessionAndStateCookieManager>();

        // Automatically add session propagation to ALL HttpClients created via IHttpClientFactory
        Services.AddSingleton<IHttpMessageHandlerBuilderFilter, SessionStateHttpMessageHandlerBuilderFilter>();

        // Event dispatcher (singleton)
        Services.TryAddSingleton<SessionStateEventDispatcher>();

        // Scoped services
        Services.TryAddScoped<SessionStateContext>();
        Services.TryAddScoped<ISessionAndStateKeyAccessor, SessionAndStateKeyAccessor>();
        Services.TryAddScoped<ISessionAndStateSessionManager, SessionAndStateSessionManager>();
        Services.TryAddScoped<ISessionStateLockManager, SessionStateLockManager>();

        // Circuit handler
        Services.TryAddScoped<SessionAndStateCircuitHandler>();
        Services.AddScoped<CircuitHandler>(sp => sp.GetRequiredService<SessionAndStateCircuitHandler>());

        // State services
        Services.TryAddScoped(typeof(SessionStateCache<>));
        Services.TryAddScoped(typeof(SessionState<>), typeof(DefaultSessionState<>));

        // Background services
        Services.AddHostedService<SessionAndStateCleanupService>();

        // Register builder for runtime access
        Services.AddSingleton(this);
    }
}
