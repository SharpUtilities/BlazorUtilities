using Microsoft.AspNetCore.Cors.Infrastructure;

namespace SessionAndState.Core.KeepAlive;

/// <summary>
/// Configuration options for the keep-alive system.
/// </summary>
public sealed class SessionAndStateKeepAliveOptions
{
    /// <summary>
    /// The keep-alive endpoint path. Default: "/session-and-state/keep-alive"
    /// </summary>
    public string Endpoint { get; set; } = "/session-and-state/keep-alive";

    /// <summary>
    /// If set, uses an existing CORS policy registered by the application.
    /// If also setting <see cref="ConfigureCors"/>, this takes precedence.
    /// </summary>
    public string? CorsPolicyName { get; set; }

    /// <summary>
    /// Optional inline CORS configuration for the keep-alive endpoint.
    /// If set, SessionAndState will register an internal named policy and apply it to the endpoint.
    /// </summary>
    public Action<CorsPolicyBuilder>? ConfigureCors { get; set; }

    /// <summary>
    /// Interval between activity checks. Default: 1 minute.
    /// The keep-alive will only call the endpoint if user activity was detected since the last check.
    /// </summary>
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Whether authentication is required for the keep-alive endpoint.
    /// Default: false (allows anonymous sessions).
    /// </summary>
    public bool RequireAuthentication { get; set; }

    /// <summary>
    /// Maximum number of requests allowed per check interval per User.
    /// Default: 10
    /// </summary>
    public int RateLimitPermitLimit { get; set; } = 10;

    /// <summary>
    /// Defines the behaviour when a partition key cannot be determined for rate limiting.
    /// Default: <see cref="UnknownPartitionBehavior.Reject"/>
    /// </summary>
    public UnknownPartitionBehavior UnknownPartitionBehavior { get; set; } = UnknownPartitionBehavior.Reject;
}
