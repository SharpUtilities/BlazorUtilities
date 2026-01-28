using System.ComponentModel;
using SessionState.Core.Options;

namespace SessionState.Core;

/// <summary>
/// Interface that state types must implement to be used with SessionState.
/// Combines change notification, equality comparison, and expiration policy.
/// </summary>
/// <typeparam name="T">The implementing type.</typeparam>
public interface ISessionStateType<T> : INotifyPropertyChanged, IEquatable<T>
    where T : class, ISessionStateType<T>
{
    /// <summary>
    /// Sliding expiration - resets on each access.
    /// Use <see cref="Expiration.None"/> for no sliding expiration.
    /// </summary>
    static abstract Expiration SlidingExpiration { get; }

    /// <summary>
    /// Absolute maximum lifetime regardless of activity.
    /// Use <see cref="Expiration.None"/> for no absolute limit.
    /// </summary>
    static abstract Expiration AbsoluteExpiration { get; }
}
