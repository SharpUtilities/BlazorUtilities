namespace SessionState.Internal;

/// <summary>
/// Marker class to track whether UseBlazorSessionState has been called.
/// </summary>
internal sealed class SessionStatePipelineMarker
{
    public bool IsConfigured { get; private set; }

    public void MarkAsConfigured() => IsConfigured = true;

    public void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "SessionState is not configured in the application pipeline. " +
                "Call app.UseSessionState() after UseRouting() and before MapRazorComponents().");
        }
    }
}
