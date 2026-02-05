using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.Logging;
using SessionAndState.Internal.Session;

namespace SessionAndState.Internal.Circuits;

/// <summary>
/// Captures the session context when the SignalR circuit opens.
/// The session was established during the HTTP request; this handler
/// ensures the session key is available in the SignalR scope.
/// </summary>
internal sealed partial class SessionAndStateCircuitHandler : CircuitHandler
{
    private readonly ISessionAndStateSessionManager _sessionAndStateSessionManager;
    private readonly SessionAndStateAsyncLocalAccessor _asyncLocal;
    private readonly ILogger<SessionAndStateCircuitHandler> _logger;

    public SessionAndStateCircuitHandler(
        ISessionAndStateSessionManager sessionAndStateSessionManager,
        SessionAndStateAsyncLocalAccessor asyncLocal,
        ILogger<SessionAndStateCircuitHandler> logger)
    {
        _sessionAndStateSessionManager = sessionAndStateSessionManager;
        _asyncLocal = asyncLocal;
        _logger = logger;
    }

    public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        // Re-establish the session in this new scope
        // This reads from cookies/claims that were set during the HTTP request
        await _sessionAndStateSessionManager.EstablishSessionAsync(cancellationToken);

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
