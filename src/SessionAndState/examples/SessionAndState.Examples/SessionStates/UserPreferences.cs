// BlazorState.Examples/SessionStates/UserPreferences.cs

using SessionAndState.Core;
using SessionAndState.Core.Options;

namespace SessionAndState.Examples.SessionStates;

public sealed class UserPreferences : SessionStateTypeBase<UserPreferences>, ISessionStateType<UserPreferences>
{
    public static Expiration SlidingExpiration => Expiration.None;
    public static Expiration AbsoluteExpiration => Expiration.None;

    private string _theme = "light";
    private string _language = "en";
    private int _fontSize = 16;
    private bool _notificationsEnabled = true;

    public string Theme
    {
        get => _theme;
        set => SetField(ref _theme, value);
    }

    public string Language
    {
        get => _language;
        set => SetField(ref _language, value);
    }

    public int FontSize
    {
        get => _fontSize;
        set => SetField(ref _fontSize, value);
    }

    public bool NotificationsEnabled
    {
        get => _notificationsEnabled;
        set => SetField(ref _notificationsEnabled, value);
    }

    public override bool Equals(UserPreferences? other)
    {
        if (other is null) return false;
        return Theme == other.Theme &&
               Language == other.Language &&
               FontSize == other.FontSize &&
               NotificationsEnabled == other.NotificationsEnabled;
    }

    public override int GetHashCode() => HashCode.Combine(Theme, Language, FontSize, NotificationsEnabled);
}
