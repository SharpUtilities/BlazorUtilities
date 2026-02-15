namespace SessionAndState.Internal;

internal sealed class SessionAndStateSessionConfigurationMarker
{
    public bool AnonymousCookieEnabled { get; set; }
    public bool AuthCookieClaimEnabled { get; set; }

    public void EnsureValid()
    {
        if (!AnonymousCookieEnabled && !AuthCookieClaimEnabled)
        {
            throw new InvalidOperationException(
                "SessionAndState requires at least one session key transport to be enabled. " +
                "Call builder.WithAnonymousCookieSession(...) and/or builder.WithAuthCookieClaimSessionKey(...).");
        }
    }
}
