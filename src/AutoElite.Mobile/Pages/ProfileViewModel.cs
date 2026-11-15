using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Session;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The profile of the user: who they are, read from the API or the device, with their photo, which they change or
/// remove; their name, phone and the language of their notifications, which they correct; the way to a new password or
/// email address; the school they are signed in to, which they can change; the language of the application; what the
/// application is; and signing out. Changing the school or signing out asks first when actions would be lost.
/// </summary>
public sealed class ProfileViewModel : ViewModel
{
    private const string SourceCode = "https://github.com/Obj-Proger/AutoElite";

    private readonly UserSession _session;
    private readonly UserAccount _account;
    private Guid? _userId;
    private Guid? _photoId;

    // The photo shown, which is downloaded again only when the profile names another; a new photo is a new document.
    private Guid? _shownPhotoId;

    /// <summary>Initializes a new instance of the <see cref="ProfileViewModel"/> class.</summary>
    /// <param name="session">The user of the application.</param>
    /// <param name="account">The account of the user, which they change.</param>
    public ProfileViewModel(UserSession session, UserAccount account)
    {
        _session = session;
        _account = account;
        NotificationLanguages = [AppTexts.Get("Settings.Russian"), AppTexts.Get("Settings.English")];
        SaveCommand = new Command(async () => await SaveAsync(), () => !IsBusy);
        ChangePhotoCommand = new Command(async () => await ChangePhotoAsync(), () => !IsBusy);
        RemovePhotoCommand = new Command(async () => await RemovePhotoAsync(), () => !IsBusy);
        SecurityCommand = new Command(async () => await Routes.OpenAsync(Routes.Security));
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

    /// <summary>Gets the full name of the user.</summary>
    public string Name
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the email address of the user.</summary>
    public string Email
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the roles of the user, by name.</summary>
    public string Roles
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the initials of the user, shown without a photo.</summary>
    public string Initials
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the photo of the user, or <see langword="null"/> without one.</summary>
    public ImageSource? Photo
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasPhoto));
        }
    }

    /// <summary>Gets a value indicating whether the user has a photo.</summary>
    public bool HasPhoto => Photo is not null;

    /// <summary>Gets or sets the last name being edited.</summary>
    public string LastName
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets or sets the first name being edited.</summary>
    public string FirstName
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets or sets the middle name being edited.</summary>
    public string MiddleName
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets or sets the phone being edited.</summary>
    public string Phone
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the languages of the notifications and emails: Russian, then English.</summary>
    public IReadOnlyList<string> NotificationLanguages { get; }

    /// <summary>Gets or sets the language of the notifications and emails, by its position.</summary>
    public int NotificationLanguageIndex
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>Gets how saving or changing the photo went, or <see langword="null"/>.</summary>
    public string? Message
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasMessage));
        }
    }

    /// <summary>Gets a value indicating whether there is a message.</summary>
    public bool HasMessage => Message is not null;

    /// <summary>Gets the command that saves the name, the phone and the language of the notifications.</summary>
    public Command SaveCommand { get; }

    /// <summary>Gets the command that chooses a new photo.</summary>
    public Command ChangePhotoCommand { get; }

    /// <summary>Gets the command that removes the photo.</summary>
    public Command RemovePhotoCommand { get; }

    /// <summary>Gets the command that opens the change of the password and of the email address.</summary>
    public Command SecurityCommand { get; }

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

    /// <summary>Gets a value indicating whether the profile is being saved or the user signed out.</summary>
    public bool IsBusy
    {
        get;
        private set
        {
            if (!SetProperty(ref field, value))
                return;

            ChangeSchoolCommand.ChangeCanExecute();
            SignOutCommand.ChangeCanExecute();
            SaveCommand.ChangeCanExecute();
            ChangePhotoCommand.ChangeCanExecute();
            RemovePhotoCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets the command that signs out and asks for the address of another school.</summary>
    public Command ChangeSchoolCommand { get; }

    /// <summary>Gets the command that signs out.</summary>
    public Command SignOutCommand { get; }

    /// <summary>Gets the command that opens the source code of the application.</summary>
    public Command SourceCodeCommand { get; }

    /// <summary>Reads the profile of the user from the API or, without a network, from the device.</summary>
    /// <returns>A task that completes when the profile is shown.</returns>
    public async Task LoadAsync()
    {
        try
        {
            Show((await _session.GetProfileAsync(CancellationToken.None)).Value);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            // Nothing to show yet: the rest of the page works without the profile.
        }

        await ShowPhotoAsync();
    }

    // The initials of a name: the first letters of the first and the last name.
    private static string InitialsOf(string? firstName, string? lastName) =>
        string.Concat(new[] { firstName, lastName }.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part![0]))
            .ToUpper(CultureInfo.CurrentCulture);

    private void Show(UserProfile profile)
    {
        _userId = profile.Id;
        _photoId = profile.PhotoDocumentId;
        Name = string.Join(
            ' ', new[] { profile.LastName, profile.FirstName, profile.MiddleName }.Where(part => !string.IsNullOrWhiteSpace(part)));
        Initials = InitialsOf(profile.FirstName, profile.LastName);
        Email = profile.Email ?? string.Empty;
        Roles = string.Join(", ", (profile.Roles ?? []).OfType<UserRole>().Select(RoleTexts.Of));
        LastName = profile.LastName ?? string.Empty;
        FirstName = profile.FirstName ?? string.Empty;
        MiddleName = profile.MiddleName ?? string.Empty;
        Phone = profile.Phone ?? string.Empty;
        NotificationLanguageIndex = profile.PreferredLanguage == Language.English ? 1 : 0;
    }

    // Shows the photo of the profile, downloaded once for each photo; without a network the initials stay.
    private async Task ShowPhotoAsync()
    {
        if (_photoId == _shownPhotoId)
            return;

        if (_photoId is not { } photoId)
        {
            Photo = null;
            _shownPhotoId = null;

            return;
        }

        try
        {
            var bytes = await _account.ReadPhotoAsync(photoId, CancellationToken.None);
            Photo = ImageSource.FromStream(() => new MemoryStream(bytes));
            _shownPhotoId = photoId;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            Photo = null;
        }
    }

    // Runs a change of the account and reads the profile again; a refusal or no network becomes the message.
    private async Task ChangeAsync(Func<Task> change, string done)
    {
        IsBusy = true;
        Message = null;
        try
        {
            await change();
            Show((await _session.GetProfileAsync(CancellationToken.None)).Value);
            await ShowPhotoAsync();
            Message = done;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException
            or InvalidOperationException)
        {
            Message = ProblemTexts.Of(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Task SaveAsync() => ChangeAsync(
        () => _account.UpdateProfileAsync(
            LastName,
            FirstName,
            MiddleName,
            Phone,
            NotificationLanguageIndex == 1 ? Language.English : Language.Russian,
            CancellationToken.None),
        AppTexts.Get("Profile.Saved"));

    private async Task ChangePhotoAsync()
    {
        if (_userId is not { } userId)
            return;

        FileResult? image;
        try
        {
            image = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = AppTexts.Get("Profile.ChoosePhoto"),
                FileTypes = FilePickerFileType.Images
            });
        }
        catch (PermissionException)
        {
            Message = AppTexts.Get("Profile.NoPhotoAccess");

            return;
        }

        if (image is null)
            return;

        await ChangeAsync(() => _account.SetPhotoAsync(userId, image, CancellationToken.None), AppTexts.Get("Profile.PhotoChanged"));
    }

    private async Task RemovePhotoAsync()
    {
        if (!await Shell.Current.DisplayAlertAsync(
                AppTexts.Get("Profile.RemovePhotoTitle"),
                AppTexts.Get("Profile.RemovePhotoQuestion"),
                AppTexts.Get("Profile.RemovePhoto"),
                AppTexts.Get("Common.Cancel")))
        {
            return;
        }

        await ChangeAsync(() => _account.RemovePhotoAsync(CancellationToken.None), AppTexts.Get("Profile.PhotoRemoved"));
    }

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
