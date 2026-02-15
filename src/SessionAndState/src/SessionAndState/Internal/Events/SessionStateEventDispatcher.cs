using Microsoft.Extensions.Logging;

namespace SessionAndState.Internal.Events;

/// <summary>
/// Internal implementation that allows raising events.
/// Singleton - shared across all scopes.
/// </summary>
internal sealed partial class SessionStateEventDispatcher : ISessionAndStateEventDispatcher
{
    private readonly SessionAndStateTimeProvider _timeProvider;
    private readonly ILogger<SessionStateEventDispatcher> _logger;

    /// <summary>
    /// Unique identifier for this application instance.
    /// Used by distributed backends to filter out self-triggered events.
    /// </summary>
    public Guid InstanceId { get; } = Guid.NewGuid();

    /// <summary>
    /// Whether to include source instance in backend events.
    /// False for in-memory (all caches notified), true for distributed.
    /// </summary>
    public bool IncludeSourceInstance { get; set; } = false;

    public SessionStateEventDispatcher(
        SessionAndStateTimeProvider timeProvider,
        ILogger<SessionStateEventDispatcher> logger)
    {
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public event EventHandler<SessionStateValueSetEventArgs>? ValueSet;
    public event EventHandler<SessionStateValueClearedEventArgs>? ValueCleared;
    public event EventHandler<SessionStateValueRefreshedEventArgs>? ValueRefreshed;
    public event EventHandler<SessionStateValueChangedEventArgs>? ValueChanged;
    public event EventHandler<SessionStateBackendEventArgs>? BackendOperation;

    internal void RaiseValueSet(string sessionKey, Type stateType, object value, bool isUpdate)
    {
        var args = new SessionStateValueSetEventArgs
        {
            SessionKey = sessionKey,
            StateType = stateType,
            Timestamp = _timeProvider.GetUtcNow(),
            Value = value,
            IsUpdate = isUpdate
        };

        LogEventRaised("ValueSet", stateType.Name, FormatKey(sessionKey));
        SafeInvoke(ValueSet, args);
    }

    internal void RaiseValueCleared(string sessionKey, Type stateType, object? previousValue)
    {
        var args = new SessionStateValueClearedEventArgs
        {
            SessionKey = sessionKey,
            StateType = stateType,
            Timestamp = _timeProvider.GetUtcNow(),
            PreviousValue = previousValue
        };

        LogEventRaised("ValueCleared", stateType.Name, FormatKey(sessionKey));
        SafeInvoke(ValueCleared, args);
    }

    internal void RaiseValueRefreshed(string sessionKey, Type stateType, DateTimeOffset? newExpiry)
    {
        var args = new SessionStateValueRefreshedEventArgs
        {
            SessionKey = sessionKey,
            StateType = stateType,
            Timestamp = _timeProvider.GetUtcNow(),
            NewExpiry = newExpiry
        };

        LogEventRaised("ValueRefreshed", stateType.Name, FormatKey(sessionKey));
        SafeInvoke(ValueRefreshed, args);
    }

    internal void RaiseValueChanged(string sessionKey, Type stateType, object value, string propertyName)
    {
        var args = new SessionStateValueChangedEventArgs
        {
            SessionKey = sessionKey,
            StateType = stateType,
            Timestamp = _timeProvider.GetUtcNow(),
            Value = value,
            PropertyName = propertyName
        };

        LogEventRaised("ValueChanged", stateType.Name, FormatKey(sessionKey));
        SafeInvoke(ValueChanged, args);
    }

    internal void RaiseBackendOperation(
        string sessionKey,
        Type stateType,
        SessionStateBackendOperation operation)
    {
        var args = new SessionStateBackendEventArgs
        {
            SessionKey = sessionKey,
            StateType = stateType,
            Timestamp = _timeProvider.GetUtcNow(),
            Operation = operation,
            SourceInstance = IncludeSourceInstance
                ? SessionStateSourceInstance.From(InstanceId)
                : SessionStateSourceInstance.None
        };

        LogBackendEventRaised(operation.ToString(), stateType.Name, FormatKey(sessionKey));
        SafeInvoke(BackendOperation, args);
    }

    private void SafeInvoke<TArgs>(EventHandler<TArgs>? handler, TArgs args) where TArgs : EventArgs
    {
        if (handler is null)
        {
            return;
        }

        foreach (var invocation in handler.GetInvocationList().Cast<EventHandler<TArgs>>())
        {
            try
            {
                invocation(this, args);
            }
            catch (Exception ex)
            {
                LogEventHandlerError(ex);
            }
        }
    }

    private static string FormatKey(string key) => SessionKeyLogHelper.Format(key);

    [LoggerMessage(LogLevel.Debug, "SessionAndState event {eventName} raised for {typeName} (key: {key})")]
    partial void LogEventRaised(string eventName, string typeName, string key);

    [LoggerMessage(LogLevel.Debug, "SessionAndState backend event {operation} for {typeName} (key: {key})")]
    partial void LogBackendEventRaised(string operation, string typeName, string key);

    [LoggerMessage(LogLevel.Error, "Error in SessionAndState event handler")]
    partial void LogEventHandlerError(Exception ex);
}
