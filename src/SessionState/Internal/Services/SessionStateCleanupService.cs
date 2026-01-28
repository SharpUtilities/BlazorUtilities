using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SessionState.Core;
using SessionState.Core.Events;
using SessionState.Core.Options;
using SessionState.Internal.Events;

namespace SessionState.Internal.Services;

/// <summary>
/// Background service that periodically cleans up expired session state entries.
/// Raises expiration events for each removed entry.
/// </summary>
internal sealed partial class SessionStateCleanupService : BackgroundService
{
    private readonly TimeSpan _interval;
    private readonly ISessionStateBackend _backend;
    private readonly SessionStateEventDispatcher _eventDispatcher;
    private readonly ILogger<SessionStateCleanupService> _logger;

    public SessionStateCleanupService(
        ISessionStateBackend backend,
        SessionStateEventDispatcher eventDispatcher,
        IOptions<SessionStateOptions> options,
        ILogger<SessionStateCleanupService> logger)
    {
        _backend = backend;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
        _interval = options.Value.CleanupInterval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogServiceStarted(_interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, stoppingToken);

                var expiredEntries = await _backend.RemoveExpiredAsync(stoppingToken);

                if (expiredEntries.Count > 0)
                {
                    LogCleanedUpExpiredStates(expiredEntries.Count);

                    foreach (var entry in expiredEntries)
                    {
                        RaiseExpirationEvents(entry);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogCleanupError(ex);
            }
        }

        LogServiceStopped();
    }

    private void RaiseExpirationEvents(ExpiredSessionStateInfo entry)
    {
        try
        {
            _eventDispatcher.RaiseValueCleared(entry.SessionKey, entry.StateType, entry.Value);
            _eventDispatcher.RaiseBackendOperation(entry.SessionKey, entry.StateType, SessionStateBackendOperation.Expired);
        }
        catch (Exception ex)
        {
            LogEventRaiseError(entry.StateType.Name, ex);
        }
    }

    [LoggerMessage(LogLevel.Information, "Session state cleanup service started, interval: {Interval}")]
    partial void LogServiceStarted(TimeSpan interval);

    [LoggerMessage(LogLevel.Information, "Cleaned up {Count} expired session states")]
    partial void LogCleanedUpExpiredStates(int count);

    [LoggerMessage(LogLevel.Error, "Error during session state cleanup")]
    partial void LogCleanupError(Exception ex);

    [LoggerMessage(LogLevel.Error, "Error raising expiration event for {TypeName}")]
    partial void LogEventRaiseError(string typeName, Exception ex);

    [LoggerMessage(LogLevel.Information, "Session state cleanup service stopped")]
    partial void LogServiceStopped();
}
