using System.ComponentModel;
using BlazorState.Core.Options;

namespace BlazorState.Core;

/// <summary>
/// Interface that state types must implement to be used with BlazorState.
/// Combines change notification, equality comparison, and expiration policy.
/// </summary>
/// <typeparam name="T">The implementing type.</typeparam>
public interface IBlazorStateType<T> : INotifyPropertyChanged, IEquatable<T>
    where T : class, IBlazorStateType<T>
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
