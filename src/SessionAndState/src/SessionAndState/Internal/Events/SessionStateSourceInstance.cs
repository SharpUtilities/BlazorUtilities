namespace SessionAndState.Internal.Events;

/// <summary>
/// Represents the source instance that triggered a backend operation.
/// Used for distributed cache invalidation scenarios.
/// </summary>
public readonly struct SessionStateSourceInstance : IEquatable<SessionStateSourceInstance>
{
    private readonly Guid _id;
    private readonly bool _hasValue;

    private SessionStateSourceInstance(Guid id)
    {
        _id = id;
        _hasValue = true;
    }

    /// <summary>
    /// No source instance specified. All caches will be notified.
    /// </summary>
    public static SessionStateSourceInstance None => default;

    /// <summary>
    /// Creates a source instance with the specified ID.
    /// </summary>
    public static SessionStateSourceInstance From(Guid id)
    {
        return id == Guid.Empty
            ? throw new ArgumentException("ID cannot be empty.", nameof(id))
            : new SessionStateSourceInstance(id);
    }

    /// <summary>
    /// Creates a new source instance with a randomly generated ID.
    /// </summary>
    public static SessionStateSourceInstance New() => new(Guid.NewGuid());

    /// <summary>
    /// Whether this represents an actual source instance.
    /// </summary>
    public bool HasValue => _hasValue;

    /// <summary>
    /// The instance ID.
    /// </summary>
    public Guid Id => _id;

    /// <summary>
    /// Checks if this source instance matches the specified ID.
    /// Returns false if this instance has no value.
    /// </summary>
    public bool Matches(Guid id) => _hasValue && _id == id;

    public bool Equals(SessionStateSourceInstance other) => _hasValue == other._hasValue && _id == other._id;

    public override bool Equals(object? obj) => obj is SessionStateSourceInstance other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(_hasValue, _id);

    public override string ToString() => _hasValue ? _id.ToString() : "(none)";

    public static bool operator ==(SessionStateSourceInstance left, SessionStateSourceInstance right) => left.Equals(right);

    public static bool operator !=(SessionStateSourceInstance left, SessionStateSourceInstance right) => !left.Equals(right);
}
