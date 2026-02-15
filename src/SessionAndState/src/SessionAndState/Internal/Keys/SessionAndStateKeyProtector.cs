using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using SessionAndState.Core.Options;

namespace SessionAndState.Internal.Keys;

internal sealed class SessionAndStateKeyProtector : ISessionAndStateKeyProtector
{
    private readonly IDataProtector _protector;
    private readonly SessionAndStateTimeProvider _timeProvider;
    private readonly SessionAndStateOptions _sessionAndStateOptions;

    public SessionAndStateKeyProtector(
        IDataProtectionProvider dataProtection,
        IOptions<SessionAndStateOptions> options,
        SessionAndStateTimeProvider timeProvider)
    {
        _protector = dataProtection.CreateProtector(options.Value.DataProtectionPurpose);
        _timeProvider = timeProvider;
        _sessionAndStateOptions = options.Value;
    }

    public string Protect(string sessionKey)
    {
        return _protector.Protect(sessionKey);
    }

    public string? Unprotect(string protectedValue)
    {
        try
        {
            return _protector.Unprotect(protectedValue);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}
