using BlazorState.Internal.Session;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Logging;

namespace BlazorState.Internal.Circuits;

/// <summary>
/// Captures the session context when the SignalR circuit opens.
/// The session was established during the HTTP request; this handler
/// ensures the session key is available in the SignalR scope.
/// </summary>
internal sealed partial class BlazorStateCircuitHandler : CircuitHandler
{
    private readonly IBlazorStateSessionManager _blazorStateSessionManager;
    private readonly BlazorStateAsyncLocalAccessor _asyncLocal;
    private readonly ILogger<BlazorStateCircuitHandler> _logger;

    public BlazorStateCircuitHandler(
        IBlazorStateSessionManager blazorStateSessionManager,
        BlazorStateAsyncLocalAccessor asyncLocal,
        ILogger<BlazorStateCircuitHandler> logger)
    {
        _blazorStateSessionManager = blazorStateSessionManager;
        _asyncLocal = asyncLocal;
        _logger = logger;
    }

    public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        // Re-establish the session in this new scope
        // This reads from cookies/claims that were set during the HTTP request
        await _blazorStateSessionManager.EstablishSessionAsync(cancellationToken);

        LogCircuitOpened(circuit.Id);
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        // Clear async local when circuit closes
        _asyncLocal.Clear();

        LogCircuitClosed(circuit.Id);
        return Task.CompletedTask;
    }

    [LoggerMessage(LogLevel.Debug, "Circuit {CircuitId} opened, session established")]
    partial void LogCircuitOpened(string circuitId);

    [LoggerMessage(LogLevel.Debug, "Circuit {CircuitId} closed")]
    partial void LogCircuitClosed(string circuitId);
}
