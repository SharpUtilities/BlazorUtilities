using Microsoft.Extensions.DependencyInjection;
using SessionAndState.Core.KeepAlive;
using SessionAndState.Core.Options;

namespace SessionAndState.Core.Builder;

public interface ISessionStateBuilder
{
    IServiceCollection Services { get; }

    ISessionStateBuilder WithInMemoryBackend();

    ISessionStateBuilder WithBackend<TBackend>()
        where TBackend : class, ISessionAndStateBackend;

    ISessionStateBuilder WithBackend<TBackend>(Func<IServiceProvider, TBackend> factory)
        where TBackend : class, ISessionAndStateBackend;

    ISessionStateBuilder WithAnonymousCookieSession(Action<AnonymousCookieSessionOptions>? configure = null);

    /// <summary>
    /// Enables storing the session key in the auth cookie as a claim (auth transport) and wires up
    /// CookieAuthenticationOptions so the claim is injected on sign-in and session is established on validation.
    /// </summary>
    /// <param name="configure">Optional options configuration (claim type).</param>
    ISessionStateBuilder WithAuthCookieClaimSessionKey(Action<AuthCookieClaimSessionKeyOptions>? configure = null);

    /// <summary>
    /// Same as <see cref="WithAuthCookieClaimSessionKey(Action{AuthCookieClaimSessionKeyOptions}?)"/> but allows specifying
    /// the cookie authentication scheme to integrate with.
    /// </summary>
    ISessionStateBuilder WithAuthCookieClaimSessionKey(
        string authenticationScheme,
        Action<AuthCookieClaimSessionKeyOptions>? configure = null);

    ISessionStateBuilder WithKeepAlive(Action<SessionAndStateKeepAliveOptions>? configure = null);
}
