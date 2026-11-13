using System.Globalization;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Session;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The home page of the signed-in user: the greeting, whether the data is from the device and how many actions wait to
/// be sent, and signing out. The schedule, the lessons and the tests come to this page next.
/// </summary>
public sealed class HomeViewModel : ViewModel
{
    private readonly UserSession _session;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="HomeViewModel"/> class.</summary>
    /// <param name="session">The user of the application.</param>
    /// <param name="timeProvider">The clock.</param>
    public HomeViewModel(UserSession session, TimeProvider timeProvider)
    {
        _session = session;
        _timeProvider = timeProvider;
        SignOutCommand = new Command(async () => await SignOutAsync(), () => !IsBusy);
    }

    /// <summary>Gets the greeting for the time of day.</summary>
    public string Greeting
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the first name of the user.</summary>
    public string Name
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets whether the data is from the device and how many actions wait to be sent, or <see langword="null"/>.</summary>
    public string? Status
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasStatus));
        }
    }

    /// <summary>Gets a value indicating whether there is a status to show.</summary>
    public bool HasStatus => Status is not null;

    /// <summary>Gets a value indicating whether the user is being signed out.</summary>
    public bool IsBusy
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                SignOutCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets the command that signs out, after asking when actions have not been sent yet.</summary>
    public Command SignOutCommand { get; }

    /// <summary>Reads the profile and the queue, each time the page appears.</summary>
    /// <returns>A task that completes when the page shows them.</returns>
    public async Task LoadAsync()
    {
        var now = _timeProvider.GetLocalNow();
        Greeting = now.Hour switch
        {
            >= 5 and < 12 => AppTexts.Get("Home.Morning"),
            >= 12 and < 18 => AppTexts.Get("Home.Afternoon"),
            _ => AppTexts.Get("Home.Evening")
        };

        var lines = new List<string>();
        try
        {
            var profile = await _session.GetProfileAsync(CancellationToken.None);
            Name = profile.Value.FirstName ?? string.Empty;
            if (profile.IsFromCache)
                lines.Add(AppTexts.Format("Home.Offline", profile.SavedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)));
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            // Neither the API nor the device has the profile; the page greets the user without their name.
            Name = string.Empty;
        }

        var waiting = await _session.CountWaitingActionsAsync(CancellationToken.None);
        if (waiting > 0)
            lines.Add(AppTexts.Format("Home.Pending", waiting));

        Status = lines.Count > 0 ? string.Join(Environment.NewLine, lines) : null;
    }

    private async Task SignOutAsync()
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
            await _session.SignOutAsync(CancellationToken.None);
            await Routes.GoToAsync(Routes.SignIn);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
