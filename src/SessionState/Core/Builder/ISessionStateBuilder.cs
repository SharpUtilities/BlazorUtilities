using Microsoft.Extensions.DependencyInjection;
using SessionState.Core.Events;
using SessionState.Core.KeepAlive;
using SessionState.Core.Options;

namespace SessionState.Core.Builder;

/// <summary>
/// Builder for configuring SessionState services.
/// </summary>
public interface ISessionStateBuilder
{
    /// <summary>
    /// The service collection being configured.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Adds the in-memory backend. Suitable for single-server deployments.
    /// </summary>
    ISessionStateBuilder WithInMemoryBackend();

    /// <summary>
    /// Adds a custom backend implementation.
    /// </summary>
    /// <typeparam name="TBackend">The backend implementation type.</typeparam>
    ISessionStateBuilder WithBackend<TBackend>()
        where TBackend : class, ISessionStateBackend;

    /// <summary>
    /// Adds a custom backend implementation with a factory.
    /// </summary>
    /// <typeparam name="TBackend">The backend implementation type.</typeparam>
    /// <param name="factory">Factory to create the backend instance.</param>
    ISessionStateBuilder WithBackend<TBackend>(Func<IServiceProvider, TBackend> factory)
        where TBackend : class, ISessionStateBackend;

    /// <summary>
    /// Enables the keep-alive endpoint and client-side service.
    /// </summary>
    /// <param name="configure">Optional keep-alive configuration.</param>
    /// <returns>The builder for chaining.</returns>
    ISessionStateBuilder WithKeepAlive(Action<SessionStateKeepAliveOptions>? configure = null);

    /// <summary>
    /// Configures event handlers for session state changes.
    /// </summary>
    /// <param name="configure">Event configuration.</param>
    ISessionStateBuilder ConfigureEvents(Action<SessionStateEventOptions> configure);

    /// <summary>
    /// Configures the core options.
    /// </summary>
    /// <param name="configure">Options configuration.</param>
    ISessionStateBuilder ConfigureOptions(Action<SessionStateOptions> configure);
}
