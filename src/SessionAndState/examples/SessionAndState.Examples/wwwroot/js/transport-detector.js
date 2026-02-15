// wwwroot/js/transport-detector.js

/**
 * Detects the actual SignalR transport being used by Blazor Server.
 * This inspects the internal Blazor/SignalR connection state.
 */
export function detectTransport() {
    // Method 1: Check Blazor's internal connection
    try {
        // Blazor Server uses an internal circuit connection
        // We can try to inspect it through various means

        // Check for active WebSocket connections
        if (checkForWebSocket()) {
            return "WebSockets";
        }

        // Check for SSE (EventSource)
        if (checkForSSE()) {
            return "ServerSentEvents";
        }

        // If neither, likely Long Polling
        if (checkForLongPolling()) {
            return "LongPolling";
        }
    } catch (e) {
        console.warn("[Transport Detector] Error during detection:", e);
    }

    // Fallback: Read from localStorage (what was configured)
    const configured = localStorage.getItem('blazor-transport');
    if (configured) {
        return configured + " (configured, detection inconclusive)";
    }

    return "Unknown (check Network tab)";
}

/**
 * Check for active WebSocket connections to _blazor
 */
function checkForWebSocket() {
    // Use Performance API to check for WebSocket connections
    if (typeof PerformanceObserver !== 'undefined') {
        const entries = performance.getEntriesByType('resource');
        for (const entry of entries) {
            if (entry.name.includes('_blazor') && entry.initiatorType === 'websocket') {
                return true;
            }
        }
    }

    // Alternative: Check if WebSocket prototype has been used recently
    // This is a heuristic and not 100% reliable
    return false;
}

/**
 * Check for active SSE (EventSource) connections
 */
function checkForSSE() {
    // EventSource connections are harder to detect directly
    // We'd need to monitor network activity
    return false;
}

/**
 * Check for Long Polling pattern (multiple sequential requests)
 */
function checkForLongPolling() {
    const entries = performance.getEntriesByType('resource');
    let blazorRequests = 0;
    const recentTime = performance.now() - 10000; // Last 10 seconds

    for (const entry of entries) {
        if (entry.name.includes('_blazor') &&
            entry.startTime > recentTime &&
            (entry.initiatorType === 'fetch' || entry.initiatorType === 'xmlhttprequest')) {
            blazorRequests++;
        }
    }

    // If we see multiple requests in a short time, it's likely long polling
    return blazorRequests >= 3;
}

/**
 * Monitor network requests to detect transport in real-time
 */
export function monitorTransport(dotNetHelper) {
    if (typeof PerformanceObserver === 'undefined') {
        console.warn("[Transport Detector] PerformanceObserver not available");
        return;
    }

    const observer = new PerformanceObserver((list) => {
        for (const entry of list.getEntries()) {
            if (entry.name.includes('_blazor')) {
                let detectedTransport = 'Unknown';

                if (entry.initiatorType === 'websocket' || entry.name.includes('ws://') || entry.name.includes('wss://')) {
                    detectedTransport = 'WebSockets';
                } else if (entry.initiatorType === 'eventsource') {
                    detectedTransport = 'ServerSentEvents';
                } else if (entry.initiatorType === 'fetch' || entry.initiatorType === 'xmlhttprequest') {
                    detectedTransport = 'LongPolling';
                }

                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync('OnTransportDetected', detectedTransport);
                }

                console.log(`[Transport Detector] Detected: ${detectedTransport}`, entry);
            }
        }
    });

    observer.observe({ entryTypes: ['resource'] });

    return () => observer.disconnect();
}

/**
 * Get transport info from localStorage
 */
export function getStoredTransport() {
    return localStorage.getItem('blazor-transport') || 'WebSockets';
}

/**
 * Save transport preference to localStorage
 */
export function saveTransport(transport) {
    localStorage.setItem('blazor-transport', transport);
    console.log(`[Transport Detector] Saved preference: ${transport}`);
}
