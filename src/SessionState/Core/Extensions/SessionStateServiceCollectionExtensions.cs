using SessionState.Core;
using SessionState.Core.Builder;
using SessionState.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering SessionState services.
/// </summary>
public static class SessionStateServiceCollectionExtensions
{
    /// <summary>
    /// Adds SessionState services.
    /// </summary>
    public static ISessionStateBuilder AddSessionState<TKeyGenerator>(this IServiceCollection services)
        where TKeyGenerator : class, ISessionStateKeyGenerator
    {
        services.AddScoped<ISessionStateKeyGenerator, TKeyGenerator>();
        return new SessionStateBuilder(services);
    }

    /// <summary>
    /// Adds SessionState services.
    /// </summary>
    public static ISessionStateBuilder WithKeyGenerator<TKeyGenerator>(
        this IServiceCollection services,
        Func<IServiceProvider, TKeyGenerator> factory)
        where TKeyGenerator : class, ISessionStateKeyGenerator
    {
        services.AddScoped<ISessionStateKeyGenerator>(factory);
        return new SessionStateBuilder(services);
    }
}
