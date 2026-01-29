using System.Collections.Concurrent;
using BlazorState.Core;
using Microsoft.Extensions.Logging;
using StorageKey = (string SessionKey, System.Type Type);

namespace BlazorState.Internal.Backends;

/// <summary>
/// In-memory implementation of ISessionStateBackend.
/// Suitable for single-server deployments.
/// </summary>
internal sealed partial class InMemoryBlazorStateBackend : IBlazorStateBackend
{
    private readonly ConcurrentDictionary<StorageKey, BlazorStateEntry> _store = new();
    private readonly BlazorStateTimeProvider _timeProvider;
    private readonly ILogger<InMemoryBlazorStateBackend> _logger;

    public InMemoryBlazorStateBackend(
        BlazorStateTimeProvider timeProvider,
        ILogger<InMemoryBlazorStateBackend> logger)
    {
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public ValueTask<T?> GetAsync<T>(string sessionKey, CancellationToken cancellationToken = default)
        where T : class, IBlazorStateType<T>
    {
        var type = typeof(T);
        var key = (sessionKey, type);

        while (true)
        {
            if (!_store.TryGetValue(key, out var entry))
            {
                return ValueTask.FromResult<T?>(null);
            }

            var now = _timeProvider.GetUtcNow();
            if (entry.IsExpired(now))
            {
                if (_store.TryRemove(key, out _))
                {
                    LogEntryExpiredAndRemoved(type.Name);
                }

                return ValueTask.FromResult<T?>(null);
            }

            var updated = entry.WithLastAccessed(now);
            if (_store.TryUpdate(key, updated, entry))
            {
                return ValueTask.FromResult(entry.Value as T);
            }
        }
    }

    public ValueTask SetAsync<T>(string sessionKey, T value, CancellationToken cancellationToken = default)
        where T : class, IBlazorStateType<T>
    {
        var key = (sessionKey, typeof(T));
        var now = _timeProvider.GetUtcNow();

        _store.AddOrUpdate(
            key,
            _ => new BlazorStateEntry
            {
                Value = value,
                CreatedAt = now,
                LastAccessedAt = now,
                SlidingExpiration = T.SlidingExpiration,
                AbsoluteExpiration = T.AbsoluteExpiration
            },
            (_, existing) => new BlazorStateEntry
            {
                Value = value,
                CreatedAt = existing.CreatedAt,
                LastAccessedAt = now,
                SlidingExpiration = T.SlidingExpiration,
                AbsoluteExpiration = T.AbsoluteExpiration
            });

        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveAsync<T>(string sessionKey, CancellationToken cancellationToken = default)
        where T : class, IBlazorStateType<T>
    {
        var key = (sessionKey, typeof(T));
        _store.TryRemove(key, out _);
        return ValueTask.CompletedTask;
    }

    public ValueTask RefreshAsync<T>(string sessionKey, CancellationToken cancellationToken = default)
        where T : class, IBlazorStateType<T>
    {
        var key = (sessionKey, typeof(T));
        var now = _timeProvider.GetUtcNow();

        while (true)
        {
            if (!_store.TryGetValue(key, out var entry))
            {
                return ValueTask.CompletedTask;
            }

            if (entry.IsExpired(now))
            {
                return ValueTask.CompletedTask;
            }

            var updated = entry.WithLastAccessed(now);
            if (_store.TryUpdate(key, updated, entry))
            {
                return ValueTask.CompletedTask;
            }
        }
    }

    public ValueTask<IReadOnlyList<ExpiredBlazorStateInfo>> RemoveExpiredAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var expiredEntries = new List<ExpiredBlazorStateInfo>();

        foreach (var kvp in _store)
        {
            if (kvp.Value.IsExpired(now))
            {
                if (_store.TryRemove(kvp))
                {
                    expiredEntries.Add(new ExpiredBlazorStateInfo
                    {
                        SessionKey = kvp.Key.SessionKey,
                        StateType = kvp.Key.Type,
                        Value = kvp.Value.Value
                    });
                }
            }
        }

        if (expiredEntries.Count > 0)
        {
            LogRemovedExpiredEntries(expiredEntries.Count);
        }

        return ValueTask.FromResult<IReadOnlyList<ExpiredBlazorStateInfo>>(expiredEntries);
    }

    public ValueTask<IReadOnlyList<RemovedBlazorStateInfo>> RemoveAllForKeyAsync(string sessionKey, CancellationToken cancellationToken = default)
    {
        var removedEntries = new List<RemovedBlazorStateInfo>();

        foreach (var key in _store.Keys)
        {
            if (key.SessionKey == sessionKey)
            {
                if (_store.TryRemove(key, out var entry))
                {
                    removedEntries.Add(new RemovedBlazorStateInfo
                    {
                        SessionKey = sessionKey,
                        StateType = key.Type,
                        Value = entry.Value
                    });
                }
            }
        }

        if (removedEntries.Count > 0)
        {
            LogRemovedEntriesForSessionKey(removedEntries.Count);
        }

        return ValueTask.FromResult<IReadOnlyList<RemovedBlazorStateInfo>>(removedEntries);
    }

    public ValueTask MigrateSessionAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default)
    {
        if (sourceKey == destinationKey)
        {
            return ValueTask.CompletedTask;
        }

        var migratedCount = 0;

        // Find all entries for the source key
        var keysToMigrate = _store.Keys
            .Where(k => k.SessionKey == sourceKey)
            .ToList();

        foreach (var oldKey in keysToMigrate)
        {
            // Atomically remove from old key
            if (_store.TryRemove(oldKey, out var entry))
            {
                var newKey = (destinationKey, oldKey.Type);

                // Add to new key - if it already exists, the destination wins (idempotent)
                // This handles the race condition where migration happens twice
                _store.TryAdd(newKey, entry);
                migratedCount++;
            }
        }

        if (migratedCount > 0)
        {
            LogMigratedSession(migratedCount, sourceKey[..Math.Min(8, sourceKey.Length)]);
        }

        return ValueTask.CompletedTask;
    }

    [LoggerMessage(LogLevel.Debug, "Entry expired and removed for type {TypeName}")]
    partial void LogEntryExpiredAndRemoved(string typeName);

    [LoggerMessage(LogLevel.Debug, "Removed {Count} expired session state entries")]
    partial void LogRemovedExpiredEntries(int count);

    [LoggerMessage(LogLevel.Debug, "Removed {Count} entries for session key")]
    partial void LogRemovedEntriesForSessionKey(int count);

    [LoggerMessage(LogLevel.Debug, "Migrated {Count} entries from session {OldKey}...")]
    partial void LogMigratedSession(int count, string oldKey);
}
