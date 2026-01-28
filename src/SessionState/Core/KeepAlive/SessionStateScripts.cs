using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace SessionState.Core.KeepAlive;

/// <summary>
/// Component that initializes SessionState client-side scripts.
/// Add this component to your <c>MainLayout.razor</c> or <c>App.razor</c>.
/// </summary>
/// <remarks>
/// <para>
/// This component is required when using <c>WithKeepAlive()</c>. It handles
/// initialisation and cleanup of the client-side keep-alive automatically.
/// </para>
/// <para>
/// If keep-alive is not configured, this component renders nothing and has no effect.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// @using SessionState
///
/// &lt;BlazorSessionStateScripts /&gt;
///
/// &lt;main&gt;
///     @Body
/// &lt;/main&gt;
/// </code>
/// </example>
public sealed class SessionStateScripts : ComponentBase, IAsyncDisposable
{
    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    private IServiceProvider Services { get; set; } = null!;

    private ISessionStateKeepAliveService? _keepAliveService;
    private bool _initialized;

    protected override void OnInitialized()
    {
        _keepAliveService = Services.GetService(typeof(ISessionStateKeepAliveService))
            as ISessionStateKeepAliveService;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && _keepAliveService is not null && !_initialized)
        {
            await _keepAliveService.InitialiseAsync(JsRuntime);
            _initialized = true;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_initialized && _keepAliveService is not null)
        {
            try
            {
                await _keepAliveService.DisposeAsync(JsRuntime);
            }
            catch (JSDisconnectedException)
            {
                // Circuit already disconnected, ignore
            }
        }
    }
}
