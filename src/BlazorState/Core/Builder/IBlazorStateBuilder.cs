using BlazorState.Core.KeepAlive;
using BlazorState.Core.Options;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorState.Core.Builder;

/// <summary>
/// Builder for configuring BlazorState services.
/// </summary>
public interface IBlazorStateBuilder
{
    /// <summary>
    /// The service collection being configured.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Adds the in-memory backend. Suitable for single-server deployments.
    /// </summary>
    IBlazorStateBuilder WithInMemoryBackend();

    /// <summary>
    /// Adds a custom backend implementation.
    /// </summary>
    /// <typeparam name="TBackend">The backend implementation type.</typeparam>
    IBlazorStateBuilder WithBackend<TBackend>()
        where TBackend : class, IBlazorStateBackend;

    /// <summary>
    /// Adds a custom backend implementation with a factory.
    /// </summary>
    /// <typeparam name="TBackend">The backend implementation type.</typeparam>
    /// <param name="factory">Factory to create the backend instance.</param>
    IBlazorStateBuilder WithBackend<TBackend>(Func<IServiceProvider, TBackend> factory)
        where TBackend : class, IBlazorStateBackend;

    /// <summary>
    /// Enables the keep-alive endpoint and client-side service.
    /// </summary>
    /// <param name="configure">Optional keep-alive configuration.</param>
    /// <returns>The builder for chaining.</returns>
    IBlazorStateBuilder WithKeepAlive(Action<BlazorStateKeepAliveOptions>? configure = null);

    /// <summary>
    /// Configures the core options.
    /// </summary>
    /// <param name="configure">Options configuration.</param>
    IBlazorStateBuilder ConfigureOptions(Action<BlazorStateOptions> configure);
}
