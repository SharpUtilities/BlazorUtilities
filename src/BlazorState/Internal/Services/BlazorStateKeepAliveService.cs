using BlazorState.Core.KeepAlive;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace BlazorState.Internal.Services;

/// <summary>
/// Service providing embedded JavaScript for keep-alive functionality.
/// </summary>
internal sealed class BlazorStateKeepAliveService : IBlazorStateKeepAliveService
{
    private readonly BlazorStateKeepAliveOptions _options;

    public BlazorStateKeepAliveService(IOptions<BlazorStateKeepAliveOptions> options)
    {
        _options = options.Value;
    }

#if DEBUG
    private const string ScriptContent =
        //LANG=JavaScript
        """
        (() => {
            if (window.BlazorUtilitiesBlazorState) {
                return;
            }
        
            const activityEvents = ['click', 'keydown', 'scroll', 'touchstart', 'mousemove'];
        
            let intervalId = null;
            let hasActivity = false;
            let cleanup = null;
        
            const log = (message) => console.log('[BlazorUtilitiesBlazorState]', message);
        
            window.BlazorUtilitiesBlazorState = {
                init(endpoint, intervalMs) {
                    if (intervalId) {
                        return;
                    }
        
                    const onActivity = () => hasActivity = true;
                    activityEvents.forEach(e => document.addEventListener(e, onActivity, { passive: true }));
        
                    log(`Initialized with interval ${intervalMs}ms`);
        
                    intervalId = setInterval(() => {
                        log(`Tick - hasActivity: ${hasActivity}`);
                        if (!hasActivity) {
                            return;
                        }
                        hasActivity = false;
                        log(`Sending keep-alive to ${endpoint}`);
                        fetch(endpoint, { method: 'GET', credentials: 'include' })
                            .then(r => log(`Keep-alive response: ${r.status}`))
                            .catch(e => log(`Keep-alive failed: ${e}`));
                    }, intervalMs);
        
                    cleanup = () => activityEvents.forEach(e => document.removeEventListener(e, onActivity));
                },
        
                dispose() {
                    if (!intervalId) {
                        return;
                    }
                    log('Disposing');
                    clearInterval(intervalId);
                    intervalId = null;
                    cleanup?.();
                }
            };
        })();
        """;
#else
    private const string ScriptContent =
        //LANG=JavaScript
        """
        (() => {
            if (window.BlazorUtilitiesBlazorState) {
                return;
            }
        
            const activityEvents = ['click', 'keydown', 'scroll', 'touchstart', 'mousemove'];
        
            let intervalId = null;
            let hasActivity = false;
            let cleanup = null;
        
            window.BlazorUtilitiesBlazorState = {
                init(endpoint, intervalMs) {
                    if (intervalId) {
                        return;
                    }
        
                    const onActivity = () => hasActivity = true;
                    activityEvents.forEach(e => document.addEventListener(e, onActivity, { passive: true }));
        
                    intervalId = setInterval(() => {
                        if (!hasActivity) {
                            return;
                        }
                        hasActivity = false;
                        fetch(endpoint, { method: 'GET', credentials: 'include' });
                    }, intervalMs);
        
                    cleanup = () => activityEvents.forEach(e => document.removeEventListener(e, onActivity));
                },
        
                dispose() {
                    if (!intervalId) {
                        return;
                    }
                    clearInterval(intervalId);
                    intervalId = null;
                    cleanup?.();
                }
            };
        })();
        """;
#endif

    public async ValueTask InitialiseAsync(IJSRuntime jsRuntime, CancellationToken cancellationToken = default)
    {
        // ToDo: We might have issues with CSP (Content Security Policy) here.
        // If this is the case we could move this to the component
        await jsRuntime.InvokeVoidAsync("eval", cancellationToken, ScriptContent);
        await jsRuntime.InvokeVoidAsync(
            "BlazorUtilitiesBlazorState.init",
            cancellationToken,
            _options.Endpoint,
            (int)_options.CheckInterval.TotalMilliseconds);
    }

    public async ValueTask DisposeAsync(IJSRuntime jsRuntime, CancellationToken cancellationToken = default)
    {
        try
        {
            await jsRuntime.InvokeVoidAsync("BlazorUtilitiesBlazorState.dispose", cancellationToken);
        }
        catch (JSDisconnectedException)
        {
            // Circuit already disconnected, ignore
        }
    }
}
