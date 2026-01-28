using Microsoft.JSInterop;

namespace SessionState.Core.KeepAlive;

/// <summary>
/// Service for managing the client-side keep-alive functionality.
/// </summary>
/// <remarks>
/// <para>
/// In most cases, you should use the <c>&lt;SessionStateScripts /&gt;</c> component
/// instead of calling this service directly. The component handles initialisation and
/// cleanup automatically.
/// </para>
/// <para>
/// This interface is exposed for advanced scenarios where you need manual control
/// over the keep-alive lifecycle.
/// </para>
/// </remarks>
public interface ISessionStateKeepAliveService
{
    /// <summary>
    /// Initialises and starts the keep-alive on the client via JS interop.
    /// </summary>
    /// <remarks>
    /// This must be called after the first render when <see cref="IJSRuntime"/> is available.
    /// Prefer using <c>&lt;SessionStateScripts /&gt;</c> which handles this automatically.
    /// </remarks>
    ValueTask InitialiseAsync(IJSRuntime jsRuntime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops and disposes the keep-alive on the client.
    /// </summary>
    ValueTask DisposeAsync(IJSRuntime jsRuntime, CancellationToken cancellationToken = default);
}
