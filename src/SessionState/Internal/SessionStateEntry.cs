using SessionState.Core.Options;

namespace SessionState.Internal;

/// <summary>
/// Represents a stored session state entry with expiration metadata.
/// Immutable - use <see cref="WithLastAccessed"/> to create updated copies.
/// </summary>
internal sealed class SessionStateEntry
{
    public required object Value { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset LastAccessedAt { get; init; }
    public required Expiration SlidingExpiration { get; init; }
    public required Expiration AbsoluteExpiration { get; init; }

    public bool IsExpired(DateTimeOffset now)
    {
        return AbsoluteExpiration.IsExpired(CreatedAt, now) ||
               SlidingExpiration.IsExpired(LastAccessedAt, now);
    }

    /// <summary>
    /// Creates a new entry with updated last accessed time.
    /// </summary>
    public SessionStateEntry WithLastAccessed(DateTimeOffset now) => new()
    {
        Value = Value,
        CreatedAt = CreatedAt,
        LastAccessedAt = now,
        SlidingExpiration = SlidingExpiration,
        AbsoluteExpiration = AbsoluteExpiration
    };

    /// <summary>
    /// Creates a new entry with an updated value, preserving creation time.
    /// </summary>
    public SessionStateEntry WithValue(object value, DateTimeOffset now) => new()
    {
        Value = value,
        CreatedAt = CreatedAt,
        LastAccessedAt = now,
        SlidingExpiration = SlidingExpiration,
        AbsoluteExpiration = AbsoluteExpiration
    };
}
