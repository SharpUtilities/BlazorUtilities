using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SessionAndState.Core;

/// <summary>
/// Convenient base class for session state types.
/// Provides INotifyPropertyChanged implementation and helper methods.
/// </summary>
/// <remarks>
/// You must still implement the static expiration properties from <see cref="ISessionStateType{T}"/>
/// as C# does not allow static virtual members in abstract classes.
/// </remarks>
/// <typeparam name="T">The derived type. Must inherit from this base class.</typeparam>
public abstract class SessionStateTypeBase<T> : INotifyPropertyChanged, IEquatable<T>
    where T : SessionStateTypeBase<T>, ISessionStateType<T>
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<TField>(ref TField field, TField value, [CallerMemberName] string? propertyName = null)
    {
        // ToDo: EqualityComparer can be expensive and or not work correctly. Change this.
        if (EqualityComparer<TField>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    public abstract bool Equals(T? other);
}
