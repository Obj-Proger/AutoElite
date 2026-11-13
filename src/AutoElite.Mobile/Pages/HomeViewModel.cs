using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using AutoElite.Mobile.Session;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The home page of the signed-in user: the greeting; the lessons of a student or of a driving instructor, with what
/// they can do with them (HomeViewModel.Lessons.cs); whether the data is from the device; the actions that wait to be
/// sent and those the school did not accept; and signing out.
/// </summary>
public sealed partial class HomeViewModel : ViewModel
{
    private readonly UserSession _session;
    private readonly StudentSchedule _schedule;
    private readonly InstructorSchedule _instructorSchedule;
    private readonly OfflineQueue _queue;
    private readonly OfflineSynchronizer _synchronizer;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="HomeViewModel"/> class.</summary>
    /// <param name="session">The user of the application.</param>
    /// <param name="schedule">The lessons of the student.</param>
    /// <param name="instructorSchedule">The slots of the driving instructor.</param>
    /// <param name="queue">The actions that wait to be sent.</param>
    /// <param name="synchronizer">Sends the actions.</param>
    /// <param name="timeProvider">The clock.</param>
    public HomeViewModel(
        UserSession session,
        StudentSchedule schedule,
        InstructorSchedule instructorSchedule,
        OfflineQueue queue,
        OfflineSynchronizer synchronizer,
        TimeProvider timeProvider)
    {
        _session = session;
        _schedule = schedule;
        _instructorSchedule = instructorSchedule;
        _queue = queue;
        _synchronizer = synchronizer;
        _timeProvider = timeProvider;
        SignOutCommand = new Command(async () => await SignOutAsync(), () => !IsBusy);
        RefreshCommand = new Command(async () => await RefreshAsync());
        DismissRefusalsCommand = new Command(async () => await DismissRefusalsAsync());
        BookCommand = new Command(async () => await Routes.OpenAsync(Routes.Booking));
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

    /// <summary>Gets a value indicating whether the user is a student, who sees their lessons and books driving ones.</summary>
    public bool IsStudent
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnRolesChanged();
        }
    }

    /// <summary>Gets a value indicating whether the user is a driving instructor, who sees their practice slots.</summary>
    public bool IsDrivingInstructor
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnRolesChanged();
        }
    }

    /// <summary>Gets a value indicating whether the page shows lessons.</summary>
    public bool ShowsLessons => IsStudent || IsDrivingInstructor;

    /// <summary>Gets a value indicating whether the user has pages that come next, as a theory instructor has.</summary>
    public bool ShowsComing => !ShowsLessons;

    /// <summary>Gets the lessons of the user, the earliest first.</summary>
    public IReadOnlyList<ScheduleEntry> Lessons
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasNoLessons));
        }
    } = [];

    /// <summary>Gets a value indicating whether the user has no lessons ahead.</summary>
    public bool HasNoLessons => ShowsLessons && Lessons.Count == 0;

    /// <summary>Gets the actions the school did not accept, each with the reason.</summary>
    public IReadOnlyList<string> Refusals
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasRefusals));
        }
    } = [];

    /// <summary>Gets a value indicating whether the school did not accept some actions.</summary>
    public bool HasRefusals => Refusals.Count > 0;

    /// <summary>Gets or sets a value indicating whether the page is being refreshed by pulling it down.</summary>
    public bool IsRefreshing
    {
        get;
        set => SetProperty(ref field, value);
    }

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

    /// <summary>Gets the command that sends the waiting actions and reads everything again.</summary>
    public Command RefreshCommand { get; }

    /// <summary>Gets the command that opens the booking of a driving lesson.</summary>
    public Command BookCommand { get; }

    /// <summary>Gets the command that removes the actions the school did not accept, once the user has seen them.</summary>
    public Command DismissRefusalsCommand { get; }

    /// <summary>Reads the profile, the lessons and the queue, each time the page appears.</summary>
    /// <returns>A task that completes when the page shows them.</returns>
    public async Task LoadAsync()
    {
        Greeting = _timeProvider.GetLocalNow().Hour switch
        {
            >= 5 and < 12 => AppTexts.Get("Home.Morning"),
            >= 12 and < 18 => AppTexts.Get("Home.Afternoon"),
            _ => AppTexts.Get("Home.Evening")
        };

        DateTimeOffset? offlineSince = null;
        try
        {
            var profile = await _session.GetProfileAsync(CancellationToken.None);
            Name = profile.Value.FirstName ?? string.Empty;
            IsStudent = profile.Value.Roles?.Contains(UserRole.Student) == true;
            IsDrivingInstructor = profile.Value.Roles?.Contains(UserRole.DrivingInstructor) == true;
            if (profile.IsFromCache)
                offlineSince = profile.SavedAt;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            // Neither the API nor the device has the profile; the page greets the user without their name.
            Name = string.Empty;
        }

        var actions = await _queue.ListAsync(CancellationToken.None);
        var waitingSlots = actions.Where(action => !action.IsRejected)
            .Select(action => SlotOf(action.Action))
            .OfType<Guid>()
            .ToHashSet();
        var lessons = new List<ScheduleEntry>();
        if (IsStudent)
            offlineSince = Earliest(offlineSince, await LoadStudentLessonsAsync(lessons, waitingSlots));

        if (IsDrivingInstructor)
            offlineSince = Earliest(offlineSince, await LoadInstructorLessonsAsync(lessons, waitingSlots));

        Lessons = [.. lessons.OrderBy(entry => entry.Start)];

        Refusals = [.. actions.Where(action => action.IsRejected).Select(PendingActionTexts.DescribeRefusal)];

        var lines = new List<string>();
        if (offlineSince is { } savedAt)
            lines.Add(AppTexts.Format("Home.Offline", savedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)));

        var waiting = actions.Count(action => !action.IsRejected);
        if (waiting > 0)
            lines.Add(AppTexts.Format("Home.Pending", waiting));

        Status = lines.Count > 0 ? string.Join(Environment.NewLine, lines) : null;
    }

    private static DateTimeOffset? Earliest(DateTimeOffset? first, DateTimeOffset? second) =>
        first is { } a && second is { } b ? (a < b ? a : b) : first ?? second;

    private void OnRolesChanged()
    {
        OnPropertyChanged(nameof(ShowsLessons));
        OnPropertyChanged(nameof(ShowsComing));
        OnPropertyChanged(nameof(HasNoLessons));
    }

    private async Task RefreshAsync()
    {
        try
        {
            await _synchronizer.SynchronizeAsync(CancellationToken.None);
            await LoadAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async Task DismissRefusalsAsync()
    {
        foreach (var action in (await _queue.ListAsync(CancellationToken.None)).Where(action => action.IsRejected))
            await _queue.DismissAsync(action.Id, CancellationToken.None);

        await LoadAsync();
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
