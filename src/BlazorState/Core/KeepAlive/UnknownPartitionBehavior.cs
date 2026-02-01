namespace BlazorState.Core.KeepAlive;

/// <summary>
/// Defines the behaviour when a partition key cannot be determined for rate limiting.
/// </summary>
public enum UnknownPartitionBehavior
{
    /// <summary>
    /// Allow the request through without rate limiting. Logs a warning.
    /// </summary>
    Allow,

    /// <summary>
    /// Reject the request with a 429 Too Many Requests response.
    /// </summary>
    Reject
}
