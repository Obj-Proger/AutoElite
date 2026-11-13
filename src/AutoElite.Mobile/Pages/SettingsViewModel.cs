using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Session;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The settings: the school the user is signed in to, which they can change; the language of the application; what
/// the application is; and signing out. Changing the school or signing out asks first when actions would be lost.
/// </summary>
public sealed class SettingsViewModel : ViewModel
{
    private const string SourceCode = "https://github.com/Obj-Proger/AutoElite";

    private readonly UserSession _session;

    /// <summary>Initializes a new instance of the <see cref="SettingsViewModel"/> class.</summary>
    /// <param name="session">The user of the application.</param>
    public SettingsViewModel(UserSession session)
    {
        _session = session;
        Languages =
        [
            AppTexts.Get("Settings.DeviceLanguage"),
            AppTexts.Get("Settings.English"),
            AppTexts.Get("Settings.Russian")
        ];
        LanguageIndex = Math.Max(0, AppLanguage.Cultures.ToList().IndexOf(AppLanguage.Chosen));
        ChangeSchoolCommand = new Command(async () => await LeaveAsync(changeSchool: true), () => !IsBusy);
        SignOutCommand = new Command(async () => await LeaveAsync(changeSchool: false), () => !IsBusy);
        SourceCodeCommand = new Command(async () => await Launcher.Default.OpenAsync(new Uri(SourceCode)));
    }

    /// <summary>Gets the school, by the host of its API.</summary>
    public string School { get; } = ApiAddress.Current.Host;

    /// <summary>Gets the languages to choose from.</summary>
    public IReadOnlyList<string> Languages { get; }

    /// <summary>Gets or sets the chosen language, by its position among <see cref="Languages"/>.</summary>
    public int LanguageIndex
    {
        get;
        set
        {
            if (!SetProperty(ref field, value) || value < 0 || value >= AppLanguage.Cultures.Count)
                return;

            AppLanguage.Choose(AppLanguage.Cultures[value]);
            LanguageNote = AppLanguage.Chosen == AppLanguage.Started ? null : AppTexts.Get("Settings.Restart");
        }
    }

    /// <summary>Gets that a newly chosen language applies after a restart, or <see langword="null"/>.</summary>
    public string? LanguageNote
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasLanguageNote));
        }
    }

    /// <summary>Gets a value indicating whether a newly chosen language waits for a restart.</summary>
    public bool HasLanguageNote => LanguageNote is not null;

    /// <summary>Gets the version of the application.</summary>
    public string Version { get; } = AppTexts.Format("Settings.Version", AppInfo.Current.VersionString);

    /// <summary>Gets a value indicating whether the user is being signed out.</summary>
    public bool IsBusy
    {
        get;
        private set
        {
            if (!SetProperty(ref field, value))
                return;

            ChangeSchoolCommand.ChangeCanExecute();
            SignOutCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets the command that signs out and asks for the address of another school.</summary>
    public Command ChangeSchoolCommand { get; }

    /// <summary>Gets the command that signs out.</summary>
    public Command SignOutCommand { get; }

    /// <summary>Gets the command that opens the source code of the application.</summary>
    public Command SourceCodeCommand { get; }

    private async Task LeaveAsync(bool changeSchool)
    {
        if (await _session.CountWaitingActionsAsync(CancellationToken.None) > 0
            && !await Shell.Current.DisplayAlertAsync(
                AppTexts.Get("Home.SignOutTitle"),
                AppTexts.Get("Home.SignOutPending"),
                AppTexts.Get("Home.SignOut"),
                AppTexts.Get("Common.Cancel")))
        {
            return;
        }

        IsBusy = true;
        try
        {
            if (changeSchool)
            {
                await _session.ChangeSchoolAsync(CancellationToken.None);
                await Routes.GoToAsync(Routes.School);
            }
            else
            {
                await _session.SignOutAsync(CancellationToken.None);
                await Routes.GoToAsync(Routes.SignIn);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
