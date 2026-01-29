using BlazorState.Core;
using BlazorState.Core.Options;

namespace BlazorState.Examples.SessionStates;

public sealed class CounterState : BlazorStateTypeBase<CounterState>, IBlazorStateType<CounterState>
{
    public static Expiration SlidingExpiration => Expiration.AfterMinutes(5);
    public static Expiration AbsoluteExpiration => Expiration.AfterHours(1);

    private int _count;
    private DateTime _lastUpdated = DateTime.UtcNow;

    public int Count
    {
        get => _count;
        set
        {
            if (SetField(ref _count, value))
            {
                LastUpdated = DateTime.UtcNow;
            }
        }
    }

    public DateTime LastUpdated
    {
        get => _lastUpdated;
        private set => SetField(ref _lastUpdated, value);
    }

    public void Increment() => Count++;
    public void Decrement() => Count--;
    public void Reset() => Count = 0;

    public override bool Equals(CounterState? other)
    {
        if (other is null) return false;
        return Count == other.Count;
    }

    public override int GetHashCode() => Count.GetHashCode();
}
