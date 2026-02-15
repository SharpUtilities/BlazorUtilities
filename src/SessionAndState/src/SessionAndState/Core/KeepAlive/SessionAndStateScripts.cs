using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using SessionAndState.Internal;

namespace SessionAndState.Core.KeepAlive;

public class SessionAndStateScripts : ComponentBase, IAsyncDisposable
{
    private IJSObjectReference? _jsModule;
    private bool _initialized;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    private IOptions<SessionAndStateFeatureFlags> FeatureFlags { get; set; } = null!;

    [Inject]
    private IOptions<SessionAndStateKeepAliveOptions> Options { get; set; } = null!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && FeatureFlags.Value.KeepAliveEnabled && !_initialized)
        {
            _jsModule = await JsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./_content/BlazorUtilities.SessionAndState/session-and-state.js");

            var options = Options.Value;
            await _jsModule.InvokeVoidAsync("init", options.Endpoint, (int)options.CheckInterval.TotalMilliseconds);
            _initialized = true;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_jsModule is not null)
        {
            try
            {
                await _jsModule.InvokeVoidAsync("dispose");
                await _jsModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Circuit already disconnected, ignore
            }
            catch (TaskCanceledException)
            {
                // Task was canceled, likely due to circuit shutdown
            }
        }
    }
}
