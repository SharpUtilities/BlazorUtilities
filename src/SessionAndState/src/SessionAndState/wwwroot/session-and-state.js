const activityEvents = ['click', 'keydown', 'scroll', 'touchstart', 'mousemove'];

let intervalId = null;
let hasActivity = false;
let cleanup = null;

const log = (message) => console.log('[SessionAndState]', message);

export function init(endpoint, intervalMs) {
    if (intervalId) {
        log('Already initialized, skipping');
        return;
    }

    const onActivity = () => hasActivity = true;
    activityEvents.forEach(e => document.addEventListener(e, onActivity, { passive: true }));

    log(`Initialized with endpoint: ${endpoint}, interval: ${intervalMs}ms`);

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
}

export function dispose() {
    if (!intervalId) {
        return;
    }
    log('Disposing');
    clearInterval(intervalId);
    intervalId = null;
    cleanup?.();
}
