using System.Security.Cryptography;
using BlazorState.Core.Options;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace BlazorState.Internal.Keys;

internal sealed class BlazorStateKeyProtector : IBlazorStateKeyProtector
{
    private readonly IDataProtector _protector;
    private readonly BlazorStateTimeProvider _timeProvider;

    public BlazorStateKeyProtector(
        IDataProtectionProvider dataProtection,
        IOptions<BlazorStateOptions> options,
        BlazorStateTimeProvider timeProvider)
    {
        _protector = dataProtection.CreateProtector(options.Value.DataProtectionPurpose);
        _timeProvider = timeProvider;
    }

    public string Protect(string sessionKey)
    {
        var payload = $"{sessionKey}|{_timeProvider.GetUtcNow().ToUnixTimeSeconds()}";
        return _protector.Protect(payload);
    }

    public string? Unprotect(string protectedValue)
    {
        try
        {
            var payload = _protector.Unprotect(protectedValue);
            var span = payload.AsSpan();

            var separatorIndex = span.IndexOf('|');
            if (separatorIndex == -1 || separatorIndex == span.Length - 1)
            {
                return null;
            }

            var idSpan = span.Slice(0, separatorIndex);
            var timestampSpan = span.Slice(separatorIndex + 1);

            // Check there's no second separator
            if (timestampSpan.IndexOf('|') != -1)
            {
                return null;
            }

            if (long.TryParse(timestampSpan, out var timestamp))
            {
                // ToDo: This should be a setting!
                var issued = DateTimeOffset.FromUnixTimeSeconds(timestamp);
                if (_timeProvider.GetUtcNow() - issued > TimeSpan.FromDays(30))
                {
                    return null;
                }
            }

            return idSpan.ToString();
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}
