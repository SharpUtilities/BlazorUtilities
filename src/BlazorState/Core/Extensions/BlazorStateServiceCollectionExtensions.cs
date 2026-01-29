using BlazorState.Core;
using BlazorState.Core.Builder;
using BlazorState.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering BlazorState services.
/// </summary>
public static class BlazorStateServiceCollectionExtensions
{
    /// <summary>
    /// Adds BlazorState services.
    /// </summary>
    public static IBlazorStateBuilder AddBlazorState<TKeyGenerator>(this IServiceCollection services)
        where TKeyGenerator : class, IBlazorStateKeyGenerator
    {
        services.AddScoped<IBlazorStateKeyGenerator, TKeyGenerator>();
        return new BlazorStateBuilder(services);
    }

    /// <summary>
    /// Adds BlazorState services.
    /// </summary>
    public static IBlazorStateBuilder WithBlazorState<TKeyGenerator>(
        this IServiceCollection services,
        Func<IServiceProvider, TKeyGenerator> factory)
        where TKeyGenerator : class, IBlazorStateKeyGenerator
    {
        services.AddScoped<IBlazorStateKeyGenerator>(factory);
        return new BlazorStateBuilder(services);
    }
}
