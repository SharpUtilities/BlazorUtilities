using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace BlazorState.Core.Options;

/// <summary>
/// Represents an expiration policy.
/// Use <see cref="None"/> for no expiration, or <see cref="After"/> to specify a duration.
/// </summary>
/// <remarks>
/// <para>
/// When binding from configuration (e.g., appsettings.json), use one of these formats:
/// </para>
/// <list type="bullet">
///   <item><c>"None"</c> - No expiration</item>
///   <item><c>"00:30:00"</c> - TimeSpan format (30 minutes)</item>
///   <item><c>"1.00:00:00"</c> - TimeSpan format (1 day)</item>
/// </list>
/// </remarks>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[TypeConverter(typeof(ExpirationTypeConverter))]
public readonly struct Expiration : IEquatable<Expiration>, ISpanParsable<Expiration>
{
    private readonly TimeSpan _duration;

    private Expiration(TimeSpan duration, bool hasExpiration)
    {
        _duration = duration;
        Expires = hasExpiration;
    }

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
    /// Parses a string into an <see cref="Expiration"/>.
    /// </summary>
    /// <param name="s">The string to parse. Use "None" for no expiration, or a TimeSpan format.</param>
    /// <param name="provider">An optional format provider (not used).</param>
    /// <returns>The parsed expiration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="s"/> is null.</exception>
    /// <exception cref="FormatException">Thrown when the string cannot be parsed.</exception>
    public static Expiration Parse(string s, IFormatProvider? provider = null)
    {
        ArgumentNullException.ThrowIfNull(s);

        if (!TryParse(s, provider, out var result))
        {
            throw new FormatException(
                $"Cannot parse '{s}' as Expiration. Use 'None' or a TimeSpan format like '00:30:00'.");
        }

        return result;
    }

    /// <summary>
    /// Parses a span of characters into an <see cref="Expiration"/>.
    /// </summary>
    /// <param name="s">The span to parse. Use "None" for no expiration, or a TimeSpan format.</param>
    /// <param name="provider">An optional format provider (not used).</param>
    /// <returns>The parsed expiration.</returns>
    /// <exception cref="FormatException">Thrown when the span cannot be parsed.</exception>
    public static Expiration Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null)
    {
        if (!TryParse(s, provider, out var result))
        {
            throw new FormatException(
                $"Cannot parse '{s}' as Expiration. Use 'None' or a TimeSpan format like '00:30:00'.");
        }

        return result;
    }

    /// <summary>
    /// Tries to parse a string into an <see cref="Expiration"/>.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">An optional format provider (not used).</param>
    /// <param name="result">The parsed expiration, or <see cref="None"/> if parsing failed.</param>
    /// <returns>True if parsing succeeded; otherwise, false.</returns>
    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        out Expiration result)
    {
        if (s is null)
        {
            result = None;
            return false;
        }

        return TryParse(s.AsSpan(), provider, out result);
    }

    /// <summary>
    /// Tries to parse a span of characters into an <see cref="Expiration"/>.
    /// </summary>
    /// <param name="s">The span to parse.</param>
    /// <param name="provider">An optional format provider (not used).</param>
    /// <param name="result">The parsed expiration, or <see cref="None"/> if parsing failed.</param>
    /// <returns>True if parsing succeeded; otherwise, false.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Expiration result)
    {
        var trimmed = s.Trim();

        if (trimmed.IsEmpty)
        {
            result = None;
            return false;
        }

        // Check for "None" (case-insensitive)
        if (trimmed.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            result = None;
            return true;
        }

        // Try parsing as TimeSpan
        if (TimeSpan.TryParse(trimmed, CultureInfo.InvariantCulture, out var timeSpan))
        {
            if (timeSpan <= TimeSpan.Zero)
            {
                result = None;
                return false;
            }

            result = After(timeSpan);
            return true;
        }

        result = None;
        return false;
    }

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

    public override string ToString() => Expires ? _duration.ToString() : "None";

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

/// <summary>
/// Type converter for <see cref="Expiration"/> to enable configuration binding.
/// </summary>
internal sealed class ExpirationTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string stringValue)
        {
            if (Expiration.TryParse(stringValue, null, out var result))
            {
                return result;
            }

            throw new FormatException(
                $"Cannot convert '{stringValue}' to Expiration. Use 'None' or a TimeSpan format like '00:30:00'.");
        }

        return base.ConvertFrom(context, culture, value);
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is Expiration expiration)
        {
            return expiration.ToString();
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }
}
