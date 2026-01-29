using BlazorState.Core;
using BlazorState.Core.Options;

namespace BlazorState.Examples.SessionStates;

public sealed class FormWizardState : BlazorStateTypeBase<FormWizardState>, IBlazorStateType<FormWizardState>
{
    public static Expiration SlidingExpiration => Expiration.AfterMinutes(15);
    public static Expiration AbsoluteExpiration => Expiration.AfterHours(2);

    private int _currentStep = 1;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _email = string.Empty;
    private string _phone = string.Empty;
    private string _address = string.Empty;
    private bool _termsAccepted;

    public int CurrentStep
    {
        get => _currentStep;
        set => SetField(ref _currentStep, value);
    }

    public string FirstName
    {
        get => _firstName;
        set => SetField(ref _firstName, value);
    }

    public string LastName
    {
        get => _lastName;
        set => SetField(ref _lastName, value);
    }

    public string Email
    {
        get => _email;
        set => SetField(ref _email, value);
    }

    public string Phone
    {
        get => _phone;
        set => SetField(ref _phone, value);
    }

    public string Address
    {
        get => _address;
        set => SetField(ref _address, value);
    }

    public bool TermsAccepted
    {
        get => _termsAccepted;
        set => SetField(ref _termsAccepted, value);
    }

    public int TotalSteps => 4;

    public bool CanGoNext => CurrentStep < TotalSteps && IsCurrentStepValid();
    public bool CanGoPrevious => CurrentStep > 1;
    public bool IsComplete => CurrentStep == TotalSteps && TermsAccepted;

    public bool IsCurrentStepValid()
    {
        return CurrentStep switch
        {
            1 => !string.IsNullOrWhiteSpace(FirstName) && !string.IsNullOrWhiteSpace(LastName),
            2 => !string.IsNullOrWhiteSpace(Email),
            3 => !string.IsNullOrWhiteSpace(Address),
            4 => TermsAccepted,
            _ => false
        };
    }

    public void NextStep()
    {
        if (CanGoNext) CurrentStep++;
    }

    public void PreviousStep()
    {
        if (CanGoPrevious) CurrentStep--;
    }

    public override bool Equals(FormWizardState? other)
    {
        if (other is null) return false;
        return CurrentStep == other.CurrentStep &&
               FirstName == other.FirstName &&
               LastName == other.LastName &&
               Email == other.Email &&
               Phone == other.Phone &&
               Address == other.Address &&
               TermsAccepted == other.TermsAccepted;
    }

    public override int GetHashCode() =>
        HashCode.Combine(CurrentStep, FirstName, LastName, Email, Phone, Address, TermsAccepted);
}
