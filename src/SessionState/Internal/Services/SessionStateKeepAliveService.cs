using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using SessionState.Core.KeepAlive;

namespace SessionState.Internal.Services;

/// <summary>
/// Service providing embedded JavaScript for keep-alive functionality.
/// </summary>
internal sealed class SessionStateKeepAliveService : ISessionStateKeepAliveService
{
    private readonly SessionStateKeepAliveOptions _options;

    public SessionStateKeepAliveService(IOptions<SessionStateKeepAliveOptions> options)
    {
        _options = options.Value;
    }

#if DEBUG
    private const string ScriptContent =
        //LANG=JavaScript
        """
        (() => {
            if (window.BlazorUtilitiesSessionState) {
                return;
            }
        
            const activityEvents = ['click', 'keydown', 'scroll', 'touchstart', 'mousemove'];
        
            let intervalId = null;
            let hasActivity = false;
            let cleanup = null;
        
            const log = (message) => console.log('[BlazorUtilitiesSessionState]', message);
        
            window.BlazorUtilitiesSessionState = {
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
            if (window.BlazorUtilitiesSessionState) {
                return;
            }
        
            const activityEvents = ['click', 'keydown', 'scroll', 'touchstart', 'mousemove'];
        
            let intervalId = null;
            let hasActivity = false;
            let cleanup = null;
        
            window.BlazorUtilitiesSessionState = {
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
            "BlazorUtilitiesSessionState.init",
            cancellationToken,
            _options.Endpoint,
            (int)_options.CheckInterval.TotalMilliseconds);
    }

    public async ValueTask DisposeAsync(IJSRuntime jsRuntime, CancellationToken cancellationToken = default)
    {
        try
        {
            await jsRuntime.InvokeVoidAsync("BlazorUtilitiesSessionState.dispose", cancellationToken);
        }
        catch (JSDisconnectedException)
        {
            // Circuit already disconnected, ignore
        }
    }
}
