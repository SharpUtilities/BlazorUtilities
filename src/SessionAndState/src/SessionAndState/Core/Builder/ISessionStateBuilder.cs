using Microsoft.Extensions.DependencyInjection;
using SessionAndState.Core.KeepAlive;
using SessionAndState.Core.Options;

namespace SessionAndState.Core.Builder;

/// <summary>
/// Builder for configuring SessionAndState services.
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
        where TBackend : class, ISessionAndStateBackend;

    /// <summary>
    /// Adds a custom backend implementation with a factory.
    /// </summary>
    /// <typeparam name="TBackend">The backend implementation type.</typeparam>
    /// <param name="factory">Factory to create the backend instance.</param>
    ISessionStateBuilder WithBackend<TBackend>(Func<IServiceProvider, TBackend> factory)
        where TBackend : class, ISessionAndStateBackend;

    /// <summary>
    /// Enables the keep-alive endpoint and client-side service.
    /// </summary>
    /// <param name="configure">Optional keep-alive configuration.</param>
    /// <returns>The builder for chaining.</returns>
    ISessionStateBuilder WithKeepAlive(Action<SessionAndStateKeepAliveOptions>? configure = null);

    /// <summary>
    /// Configures the core options.
    /// </summary>
    /// <param name="configure">Options configuration.</param>
    ISessionStateBuilder ConfigureOptions(Action<SessionAndStateOptions> configure);
}
