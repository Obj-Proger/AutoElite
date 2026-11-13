using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Session;

namespace AutoElite.Mobile.Pages;

/// <summary>Signing in with the email address and the password, or going back to change the school.</summary>
public sealed class SignInViewModel : ViewModel
{
    private readonly UserSession _session;

    /// <summary>Initializes a new instance of the <see cref="SignInViewModel"/> class.</summary>
    /// <param name="session">The user of the application.</param>
    public SignInViewModel(UserSession session)
    {
        _session = session;
        SignInCommand = new Command(async () => await SignInAsync(), () => !IsBusy);
        ChangeSchoolCommand = new Command(async () => await ChangeSchoolAsync(), () => !IsBusy);
    }

    /// <summary>Gets the host of the school's API, so that the user sees which school they sign in to.</summary>
    public string School
    {
        get;
        private set => SetProperty(ref field, value);
    } = ApiAddress.Current.Host;

    /// <summary>Gets or sets the email address.</summary>
    public string Email
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets or sets the password, which is kept only until the user is signed in.</summary>
    public string Password
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets why signing in failed, or <see langword="null"/>.</summary>
    public string? Error
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    /// <summary>Gets a value indicating whether signing in failed.</summary>
    public bool HasError => Error is not null;

    /// <summary>Gets a value indicating whether the user is being signed in.</summary>
    public bool IsBusy
    {
        get;
        private set
        {
            if (!SetProperty(ref field, value))
                return;

            SignInCommand.ChangeCanExecute();
            ChangeSchoolCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets the command that signs in.</summary>
    public Command SignInCommand { get; }

    /// <summary>Gets the command that goes back to the address of the school.</summary>
    public Command ChangeSchoolCommand { get; }

    /// <summary>Shows the school the user signs in to, which may have changed since the page was created.</summary>
    public void Refresh() => School = ApiAddress.Current.Host;

    private async Task SignInAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrEmpty(Password))
        {
            Error = AppTexts.Get("SignIn.Missing");

            return;
        }

        Error = null;
        IsBusy = true;
        try
        {
            var outcome = await _session.SignInAsync(Email.Trim(), Password, CancellationToken.None);
            if (outcome == SignInOutcome.SignedIn)
            {
                Password = string.Empty;
                await Routes.GoToAsync(Routes.Home);

                return;
            }

            Error = outcome switch
            {
                SignInOutcome.WrongCredentials => AppTexts.Get("SignIn.WrongCredentials"),
                SignInOutcome.Blocked => AppTexts.Get("SignIn.Blocked"),
                SignInOutcome.TooManyAttempts => AppTexts.Get("SignIn.TooManyAttempts"),
                SignInOutcome.Unreachable => AppTexts.Get("SignIn.Unreachable"),
                SignInOutcome.NotForThisApp => AppTexts.Get("SignIn.NotForThisApp"),
                _ => AppTexts.Get("SignIn.Failed")
            };
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ChangeSchoolAsync()
    {
        IsBusy = true;
        try
        {
            await _session.ChangeSchoolAsync(CancellationToken.None);
            Error = null;
            await Routes.GoToAsync(Routes.School);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
