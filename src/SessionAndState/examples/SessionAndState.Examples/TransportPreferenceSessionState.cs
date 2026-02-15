// SessionStates/TransportPreference.cs
using SessionAndState.Core;
using SessionAndState.Core.Options;

namespace SessionAndState.Examples.SessionStates;

public sealed class TransportPreference : SessionStateTypeBase<TransportPreference>, ISessionStateType<TransportPreference>
{
    public static Expiration SlidingExpiration => Expiration.None;
    public static Expiration AbsoluteExpiration => Expiration.None;

    public enum TransportType
    {
        WebSockets,
        ServerSentEvents,
        LongPolling
    }

    private TransportType _selectedTransport = TransportType.WebSockets;
    private bool _hasSelected;
    private DateTime _selectedAt = DateTime.MinValue;

    public TransportType SelectedTransport
    {
        get => _selectedTransport;
        set => SetField(ref _selectedTransport, value);
    }

    public bool HasSelected
    {
        get => _hasSelected;
        set => SetField(ref _hasSelected, value);
    }

    public DateTime SelectedAt
    {
        get => _selectedAt;
        set => SetField(ref _selectedAt, value);
    }

    public override bool Equals(TransportPreference? other)
    {
        if (other is null) return false;
        return SelectedTransport == other.SelectedTransport &&
               HasSelected == other.HasSelected;
    }

    public override int GetHashCode() => HashCode.Combine(SelectedTransport, HasSelected);
}
