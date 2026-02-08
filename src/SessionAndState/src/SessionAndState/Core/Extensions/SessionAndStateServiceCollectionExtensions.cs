using SessionAndState.Core;
using SessionAndState.Core.Builder;
using SessionAndState.Core.Options;
using SessionAndState.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering SessionAndState services.
/// </summary>
public static class SessionAndStateServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public ISessionStateBuilder AddSessionAndState<TKeyGenerator>()
            where TKeyGenerator : class, ISessionAndStateKeyGenerator
        {
            services.AddScoped<ISessionAndStateKeyGenerator, TKeyGenerator>();
            return new SessionStateBuilder(services);
        }

        public ISessionStateBuilder AddSessionAndState<TKeyGenerator>(Action<SessionAndStateOptions> configure)
            where TKeyGenerator : class, ISessionAndStateKeyGenerator
        {
            ArgumentNullException.ThrowIfNull(configure);

            services.AddScoped<ISessionAndStateKeyGenerator, TKeyGenerator>();
            services.Configure(configure);

            return new SessionStateBuilder(services);
        }

        public ISessionStateBuilder AddSessionAndState<TKeyGenerator>(Func<IServiceProvider, TKeyGenerator> factory)
            where TKeyGenerator : class, ISessionAndStateKeyGenerator
        {
            services.AddScoped<ISessionAndStateKeyGenerator>(factory);
            return new SessionStateBuilder(services);
        }

        public ISessionStateBuilder AddSessionAndState<TKeyGenerator>(Func<IServiceProvider, TKeyGenerator> factory,
            Action<SessionAndStateOptions> configure)
            where TKeyGenerator : class, ISessionAndStateKeyGenerator
        {
            ArgumentNullException.ThrowIfNull(factory);
            ArgumentNullException.ThrowIfNull(configure);

            services.AddScoped<ISessionAndStateKeyGenerator>(factory);
            services.Configure(configure);

            return new SessionStateBuilder(services);
        }
    }
}
