using Microsoft.Extensions.Logging;

namespace BlazorState.Internal.Events;

/// <summary>
/// Internal implementation that allows raising events.
/// Singleton - shared across all scopes.
/// </summary>
internal sealed partial class BlazorStateEventDispatcher : IBlazorStateEventDispatcher
{
    private readonly BlazorStateTimeProvider _timeProvider;
    private readonly ILogger<BlazorStateEventDispatcher> _logger;

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

    public BlazorStateEventDispatcher(
        BlazorStateTimeProvider timeProvider,
        ILogger<BlazorStateEventDispatcher> logger)
    {
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public event EventHandler<BlazorStateValueSetEventArgs>? ValueSet;
    public event EventHandler<BlazorStateValueClearedEventArgs>? ValueCleared;
    public event EventHandler<BlazorStateValueRefreshedEventArgs>? ValueRefreshed;
    public event EventHandler<BlazorStateValueChangedEventArgs>? ValueChanged;
    public event EventHandler<BlazorStateBackendEventArgs>? BackendOperation;

    internal void RaiseValueSet(string sessionKey, Type stateType, object value, bool isUpdate)
    {
        var args = new BlazorStateValueSetEventArgs
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
        var args = new BlazorStateValueClearedEventArgs
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
        var args = new BlazorStateValueRefreshedEventArgs
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
        var args = new BlazorStateValueChangedEventArgs
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
        BlazorStateBackendOperation operation)
    {
        var args = new BlazorStateBackendEventArgs
        {
            SessionKey = sessionKey,
            StateType = stateType,
            Timestamp = _timeProvider.GetUtcNow(),
            Operation = operation,
            SourceInstance = IncludeSourceInstance
                ? BlazorStateSourceInstance.From(InstanceId)
                : BlazorStateSourceInstance.None
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

    private static string FormatKey(string key) =>
        key.Length > 8 ? key[..8] + "..." : key;

    [LoggerMessage(LogLevel.Debug, "BlazorState event {eventName} raised for {typeName} (key: {key})")]
    partial void LogEventRaised(string eventName, string typeName, string key);

    [LoggerMessage(LogLevel.Debug, "BlazorState backend event {operation} for {typeName} (key: {key})")]
    partial void LogBackendEventRaised(string operation, string typeName, string key);

    [LoggerMessage(LogLevel.Error, "Error in BlazorState event handler")]
    partial void LogEventHandlerError(Exception ex);
}
