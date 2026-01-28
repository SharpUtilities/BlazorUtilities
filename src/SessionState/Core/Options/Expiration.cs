using System.Diagnostics;

namespace SessionState.Core.Options;

/// <summary>
/// Represents an expiration policy.
/// Use <see cref="None"/> for no expiration, or <see cref="After"/> to specify a duration.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public readonly struct Expiration : IEquatable<Expiration>
{
    private readonly TimeSpan _duration;

    private Expiration(TimeSpan duration, bool hasExpiration)
    {
        _duration = duration;
        Expires = hasExpiration;
    }

    /// <summary>
    /// No expiration.
    /// </summary>
    public static Expiration None => new(TimeSpan.Zero, hasExpiration: false);

    /// <summary>
    /// Creates an expiration with the specified duration.
    /// </summary>
    /// <param name="duration">The duration. Must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when duration is zero or negative.</exception>
    public static Expiration After(TimeSpan duration)
    {
        return duration <= TimeSpan.Zero
            ? throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be positive.")
            : new Expiration(duration, hasExpiration: true);
    }

    /// <summary>
    /// Creates an expiration after the specified number of minutes.
    /// </summary>
    public static Expiration AfterMinutes(double minutes) => After(TimeSpan.FromMinutes(minutes));

    /// <summary>
    /// Creates an expiration after the specified number of minutes.
    /// </summary>
    public static Expiration AfterMinutes(long minutes) => After(TimeSpan.FromMinutes(minutes));

    /// <summary>
    /// Creates an expiration after the specified number of hours.
    /// </summary>
    public static Expiration AfterHours(double hours) => After(TimeSpan.FromHours(hours));

    /// <summary>
    /// Creates an expiration after the specified number of hours.
    /// </summary>
    public static Expiration AfterHours(int hours) => After(TimeSpan.FromHours(hours));

    /// <summary>
    /// Creates an expiration after the specified number of days.
    /// </summary>
    public static Expiration AfterDays(double days) => After(TimeSpan.FromDays(days));

    /// <summary>
    /// Creates an expiration after the specified number of days.
    /// </summary>
    public static Expiration AfterDays(int days) => After(TimeSpan.FromDays(days));

    /// <summary>
    /// Whether this expiration has a duration.
    /// </summary>
    public bool Expires { get; }

    /// <summary>
    /// Gets the duration.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when this is <see cref="None"/>.</exception>
    public TimeSpan Duration => Expires
        ? _duration
        : throw new InvalidOperationException("Cannot get duration of Expiration.None. Check Expires property first.");

    /// <summary>
    /// Gets the duration, or the specified default value if this is <see cref="None"/>.
    /// </summary>
    /// <param name="defaultValue">The value to return if this is <see cref="None"/>.</param>
    public TimeSpan GetDurationOrDefault(TimeSpan defaultValue) => Expires ? _duration : defaultValue;

    /// <summary>
    /// Tries to get the duration.
    /// </summary>
    /// <param name="duration">The duration if this expires; otherwise, <see cref="TimeSpan.Zero"/>.</param>
    /// <returns>True if this has a duration; otherwise, false.</returns>
    public bool TryGetDuration(out TimeSpan duration)
    {
        duration = _duration;
        return Expires;
    }

    /// <summary>
    /// Checks if the specified start time has expired relative to now.
    /// Returns false if this is <see cref="None"/>.
    /// </summary>
    public bool IsExpired(DateTimeOffset startTime, DateTimeOffset now)
    {
        if (!Expires)
        {
            return false;
        }

        return now > startTime.Add(_duration);
    }

    public bool Equals(Expiration other)
    {
        return Expires == other.Expires && _duration == other._duration;
    }

    public override bool Equals(object? obj)
    {
        return obj is Expiration other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Expires, _duration);
    }

    public static bool operator ==(Expiration left, Expiration right) => left.Equals(right);
    public static bool operator !=(Expiration left, Expiration right) => !left.Equals(right);

    public override string ToString() => Expires ? $"After {_duration}" : "None";

    private string DebuggerDisplay => Expires ? FormatDuration(_duration) : "None";

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
        {
            return $"{duration.TotalDays:0.##} day(s)";
        }

        if (duration.TotalHours >= 1)
        {
            return $"{duration.TotalHours:0.##} hour(s)";
        }

        return duration.TotalMinutes >= 1
            ? $"{duration.TotalMinutes:0.##} minute(s)"
            : $"{duration.TotalSeconds:0.##} second(s)";
    }
}
