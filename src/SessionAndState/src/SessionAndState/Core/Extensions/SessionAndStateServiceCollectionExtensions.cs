using SessionAndState.Core;
using SessionAndState.Core.Builder;
using SessionAndState.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering SessionAndState services.
/// </summary>
public static class SessionAndStateServiceCollectionExtensions
{
    /// <summary>
    /// Adds SessionAndState services.
    /// </summary>
    public static ISessionStateBuilder AddSessionAndState<TKeyGenerator>(this IServiceCollection services)
        where TKeyGenerator : class, ISessionAndStateKeyGenerator
    {
        services.AddScoped<ISessionAndStateKeyGenerator, TKeyGenerator>();
        return new SessionStateBuilder(services);
    }

    /// <summary>
    /// Adds SessionAndState services.
    /// </summary>
    public static ISessionStateBuilder AddSessionAndState<TKeyGenerator>(
        this IServiceCollection services,
        Func<IServiceProvider, TKeyGenerator> factory)
        where TKeyGenerator : class, ISessionAndStateKeyGenerator
    {
        services.AddScoped<ISessionAndStateKeyGenerator>(factory);
        return new SessionStateBuilder(services);
    }
}
