using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Session;
using AutoElite.Mobile.Theming;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The profile of the user as a menu: who they are, read from the API or the device, with their photo, which they change
/// or remove from the avatar; the ways to their personal details, to a new password or email address and, for a
/// student, to their contracts and documents; the language and the theme of the application; the school they are
/// signed in to, which they can change; what the application is; and signing out. Changing the school or signing out
/// asks first when actions would be lost.
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
        PhotoCommand = new Command(async () => await OfferPhotoAsync(), () => !IsBusy);
        PersonalCommand = new Command(async () => await Routes.OpenAsync(Routes.Personal));
        SecurityCommand = new Command(async () => await Routes.OpenAsync(Routes.Security));
        ContractsCommand = new Command(async () => await Routes.OpenAsync(Routes.Contracts));
        DocumentsCommand = new Command(async () => await Routes.OpenAsync(Routes.Documents));
        LanguageCommand = new Command(async () => await ChooseLanguageAsync());
        ThemeCommand = new Command<string>(ChooseTheme);
        SchoolCommand = new Command(async () => await OfferSchoolAsync(), () => !IsBusy);
        AboutCommand = new Command(async () => await ShowAboutAsync());
        SignOutCommand = new Command(async () => await LeaveAsync(changeSchool: false), () => !IsBusy);
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

    /// <summary>Gets a value indicating whether the user is a student, who has contracts and documents.</summary>
    public bool IsStudent
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets how changing the photo or the language went, or <see langword="null"/>.</summary>
    public string? Message
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets the command that offers a new photo or removing the photo.</summary>
    public Command PhotoCommand { get; }

    /// <summary>Gets the command that opens the personal details.</summary>
    public Command PersonalCommand { get; }

    /// <summary>Gets the command that opens the change of the password and of the email address.</summary>
    public Command SecurityCommand { get; }

    /// <summary>Gets the command that opens the contracts of a student.</summary>
    public Command ContractsCommand { get; }

    /// <summary>Gets the command that opens the documents of a student.</summary>
    public Command DocumentsCommand { get; }

    /// <summary>Gets the command that offers the languages of the application.</summary>
    public Command LanguageCommand { get; }

    /// <summary>Gets the command that chooses a theme, by its number: 0 the device, 1 dark, 2 light.</summary>
    public Command<string> ThemeCommand { get; }

    /// <summary>Gets the command that offers changing the school.</summary>
    public Command SchoolCommand { get; }

    /// <summary>Gets the command that tells what the application is.</summary>
    public Command AboutCommand { get; }

    /// <summary>Gets the school, by the host of its API.</summary>
    public string School { get; } = ApiAddress.Current.Host;

    /// <summary>Gets the language of the application: the chosen one, or that of the device.</summary>
    public string LanguageName
    {
        get;
        private set => SetProperty(ref field, value);
    } = NameOfChosenLanguage();

    /// <summary>Gets the theme chosen, which marks its card.</summary>
    public ThemeChoice Theme
    {
        get;
        private set
        {
            if (!SetProperty(ref field, value))
                return;

            OnPropertyChanged(nameof(IsDeviceTheme));
            OnPropertyChanged(nameof(IsDarkTheme));
            OnPropertyChanged(nameof(IsLightTheme));
        }
    } = AppPalette.Chosen;

    /// <summary>Gets a value indicating whether the theme follows the device.</summary>
    public bool IsDeviceTheme => Theme == ThemeChoice.Device;

    /// <summary>Gets a value indicating whether the theme is the dark one.</summary>
    public bool IsDarkTheme => Theme == ThemeChoice.Dark;

    /// <summary>Gets a value indicating whether the theme is the light one.</summary>
    public bool IsLightTheme => Theme == ThemeChoice.Light;

    /// <summary>Gets the version of the application, such as 1.0.</summary>
    public string VersionNumber { get; } = AppInfo.Current.VersionString;

    /// <summary>Gets a value indicating whether the profile is being saved or the user signed out.</summary>
    public bool IsBusy
    {
        get;
        private set
        {
            if (!SetProperty(ref field, value))
                return;

            SchoolCommand.ChangeCanExecute();
            SignOutCommand.ChangeCanExecute();
            PhotoCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets the command that signs out.</summary>
    public Command SignOutCommand { get; }

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

    private void Show(UserProfile profile)
    {
        _userId = profile.Id;
        _photoId = profile.PhotoDocumentId;
        Name = string.Join(
            ' ', new[] { profile.LastName, profile.FirstName, profile.MiddleName }.Where(part => !string.IsNullOrWhiteSpace(part)));
        Email = profile.Email ?? string.Empty;
        Roles = string.Join(" · ", (profile.Roles ?? []).OfType<UserRole>().Select(RoleTexts.Of));
        IsStudent = (profile.Roles ?? []).Contains(UserRole.Student);
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

    // The names of the languages to choose from: that of the device, English and Russian.
    private static string NameOfChosenLanguage() =>
        LanguageNames()[Math.Max(0, AppLanguage.Cultures.ToList().IndexOf(AppLanguage.Chosen))];

    private static string[] LanguageNames() =>
        [AppTexts.Get("Settings.DeviceLanguage"), AppTexts.Get("Settings.English"), AppTexts.Get("Settings.Russian")];

    private async Task OfferPhotoAsync()
    {
        var change = AppTexts.Get("Profile.ChangePhoto");
        var remove = AppTexts.Get("Profile.RemovePhoto");
        string[] choices = HasPhoto ? [change, remove] : [change];
        var choice = await Shell.Current.DisplayActionSheetAsync(
            AppTexts.Get("Profile.Photo"), AppTexts.Get("Common.Cancel"), null, choices);
        if (choice == change)
            await ChangePhotoAsync();
        else if (choice == remove)
            await RemovePhotoAsync();
    }

    // Offers the languages and chooses one; a language other than the one the application started in waits for a restart.
    private async Task ChooseLanguageAsync()
    {
        var names = LanguageNames();
        var choice = await Shell.Current.DisplayActionSheetAsync(
            AppTexts.Get("Settings.Language"), AppTexts.Get("Common.Cancel"), null, names);
        var index = Array.IndexOf(names, choice);
        if (index < 0 || index >= AppLanguage.Cultures.Count)
            return;

        AppLanguage.Choose(AppLanguage.Cultures[index]);
        LanguageName = NameOfChosenLanguage();
        Message = AppLanguage.Chosen == AppLanguage.Started ? null : AppTexts.Get("Settings.Restart");
    }

    private void ChooseTheme(string? choice)
    {
        if (!int.TryParse(choice, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            || !Enum.IsDefined((ThemeChoice)number)
            || (ThemeChoice)number == Theme)
        {
            return;
        }

        AppPalette.Choose((ThemeChoice)number);
        Theme = AppPalette.Chosen;
    }

    private async Task OfferSchoolAsync()
    {
        var change = AppTexts.Get("SignIn.ChangeSchool");
        if (await Shell.Current.DisplayActionSheetAsync(School, AppTexts.Get("Common.Cancel"), null, change) == change)
            await LeaveAsync(changeSchool: true);
    }

    private async Task ShowAboutAsync()
    {
        var sourceCode = AppTexts.Get("Settings.SourceCode");
        var title = string.Join(
            Environment.NewLine, AppTexts.Format("Settings.Version", VersionNumber), AppTexts.Get("Settings.License"));
        if (await Shell.Current.DisplayActionSheetAsync(title, AppTexts.Get("Common.Cancel"), null, sourceCode) == sourceCode)
            await Launcher.Default.OpenAsync(new Uri(SourceCode));
    }

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
