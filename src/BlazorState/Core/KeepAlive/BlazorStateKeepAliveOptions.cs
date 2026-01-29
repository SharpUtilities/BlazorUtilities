namespace BlazorState.Core.KeepAlive;

/// <summary>
/// Configuration options for the keep-alive system.
/// </summary>
public sealed class BlazorStateKeepAliveOptions
{
    /// <summary>
    /// The keep-alive endpoint path. Default: "/api/blazor-session-state/keep-alive"
    /// </summary>
    public string Endpoint { get; set; } = "/api/blazor-session-state/keep-alive";

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
    /// Maximum number of requests allowed per check interval per IP address.
    /// Default: 10
    /// </summary>
    public int RateLimitPermitLimit { get; set; } = 10;
}
