namespace SessionAndState.Internal;

/// <summary>
/// Marker class to track whether UseBlazorSessionState has been called.
/// </summary>
internal sealed class SessionAndStatePipelineMarker
{
    public bool IsConfigured { get; private set; }

    public void MarkAsConfigured() => IsConfigured = true;

    public void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "SessionAndState is not configured in the application pipeline. " +
                "Call app.UseBlazorState() after UseRouting() and before MapRazorComponents().");
        }
    }
}
