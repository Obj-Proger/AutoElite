using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Session;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The personal details of the user, opened from the profile: their name, phone and the language of their
/// notifications and emails, read from the API or the device, which they correct and save.
/// </summary>
public sealed class PersonalViewModel : ViewModel
{
    private readonly UserSession _session;
    private readonly UserAccount _account;

    /// <summary>Initializes a new instance of the <see cref="PersonalViewModel"/> class.</summary>
    /// <param name="session">The user of the application.</param>
    /// <param name="account">The account of the user, which they change.</param>
    public PersonalViewModel(UserSession session, UserAccount account)
    {
        _session = session;
        _account = account;
        NotificationLanguages = [AppTexts.Get("Settings.Russian"), AppTexts.Get("Settings.English")];
        SaveCommand = new Command(async () => await SaveAsync(), () => !IsBusy);
    }

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

    /// <summary>Gets how saving went, or <see langword="null"/>.</summary>
    public string? Message
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets a value indicating whether saving failed, which shows the message as a warning.</summary>
    public bool HasFailed
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets a value indicating whether the details are being saved.</summary>
    public bool IsBusy
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                SaveCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets the command that saves the name, the phone and the language of the notifications.</summary>
    public Command SaveCommand { get; }

    /// <summary>Reads the details from the API or, without a network, from the device.</summary>
    /// <returns>A task that completes when they are shown.</returns>
    public async Task LoadAsync()
    {
        try
        {
            Show((await _session.GetProfileAsync(CancellationToken.None)).Value);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            // The fields stay empty until the profile can be read.
        }
    }

    private void Show(UserProfile profile)
    {
        LastName = profile.LastName ?? string.Empty;
        FirstName = profile.FirstName ?? string.Empty;
        MiddleName = profile.MiddleName ?? string.Empty;
        Phone = profile.Phone ?? string.Empty;
        NotificationLanguageIndex = profile.PreferredLanguage == Language.English ? 1 : 0;
    }

    private async Task SaveAsync()
    {
        IsBusy = true;
        Message = null;
        try
        {
            await _account.UpdateProfileAsync(
                LastName,
                FirstName,
                MiddleName,
                Phone,
                NotificationLanguageIndex == 1 ? Language.English : Language.Russian,
                CancellationToken.None);
            Show((await _session.GetProfileAsync(CancellationToken.None)).Value);
            HasFailed = false;
            Message = AppTexts.Get("Profile.Saved");
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException
            or InvalidOperationException)
        {
            HasFailed = true;
            Message = ProblemTexts.Of(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
