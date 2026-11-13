using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;
using AutoElite.Client.Offline.Actions;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using AutoElite.Mobile.Session;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The home page of the signed-in user: the greeting; for a student, their lessons, with the booking of a driving
/// lesson that can be cancelled; whether the data is from the device; the actions that wait to be sent and those the
/// school did not accept; and signing out.
/// </summary>
public sealed class HomeViewModel : ViewModel
{
    private readonly UserSession _session;
    private readonly StudentSchedule _schedule;
    private readonly OfflineQueue _queue;
    private readonly OfflineSynchronizer _synchronizer;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="HomeViewModel"/> class.</summary>
    /// <param name="session">The user of the application.</param>
    /// <param name="schedule">The lessons of the student.</param>
    /// <param name="queue">The actions that wait to be sent.</param>
    /// <param name="synchronizer">Sends the actions.</param>
    /// <param name="timeProvider">The clock.</param>
    public HomeViewModel(
        UserSession session,
        StudentSchedule schedule,
        OfflineQueue queue,
        OfflineSynchronizer synchronizer,
        TimeProvider timeProvider)
    {
        _session = session;
        _schedule = schedule;
        _queue = queue;
        _synchronizer = synchronizer;
        _timeProvider = timeProvider;
        SignOutCommand = new Command(async () => await SignOutAsync(), () => !IsBusy);
        RefreshCommand = new Command(async () => await RefreshAsync());
        DismissRefusalsCommand = new Command(async () => await DismissRefusalsAsync());
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

    /// <summary>Gets a value indicating whether the user is a student, whose lessons the page shows.</summary>
    public bool IsStudent
    {
        get;
        private set
        {
            if (!SetProperty(ref field, value))
                return;

            OnPropertyChanged(nameof(IsNotStudent));
            OnPropertyChanged(nameof(HasNoLessons));
        }
    }

    /// <summary>Gets a value indicating whether the user is an instructor, whose pages come next.</summary>
    public bool IsNotStudent => !IsStudent;

    /// <summary>Gets the lessons of the student, the earliest first.</summary>
    public IReadOnlyList<ScheduleEntry> Lessons
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasNoLessons));
        }
    } = [];

    /// <summary>Gets a value indicating whether a student has no lessons ahead.</summary>
    public bool HasNoLessons => IsStudent && Lessons.Count == 0;

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
            if (profile.IsFromCache)
                offlineSince = profile.SavedAt;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            // Neither the API nor the device has the profile; the page greets the user without their name.
            Name = string.Empty;
        }

        var actions = await _queue.ListAsync(CancellationToken.None);
        if (IsStudent)
            offlineSince = Earliest(offlineSince, await LoadLessonsAsync(actions));
        else
            Lessons = [];

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

    private static string TimeOf(DateTimeOffset start, DateTimeOffset end)
    {
        var culture = CultureInfo.CurrentCulture;
        var localStart = start.ToLocalTime();

        return string.Create(
            culture, $"{localStart.ToString("ddd d MMM", culture)}, {localStart:t} – {end.ToLocalTime():t}");
    }

    private static string DetailsOf(params string?[] parts) =>
        string.Join(" · ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));

    // Reads the lessons and gives when they were read, if they come from the device.
    private async Task<DateTimeOffset?> LoadLessonsAsync(IReadOnlyList<PendingAction> actions)
    {
        StudentLessons lessons;
        try
        {
            lessons = await _schedule.ReadAsync(CancellationToken.None);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            // Never read on this device and no network: the page shows no lessons until it can read them.
            Lessons = [];

            return null;
        }

        var cancelling = actions.Where(action => !action.IsRejected)
            .Select(action => action.Action)
            .OfType<CancelDrivingBooking>()
            .Select(cancellation => cancellation.SlotId)
            .ToHashSet();
        var now = _timeProvider.GetUtcNow();

        Lessons =
        [
            .. lessons.Driving.Value.Select(lesson => DrivingEntry(lesson, cancelling, now)).OfType<ScheduleEntry>()
                .Concat(lessons.Theory.Value.Select(TheoryEntry).OfType<ScheduleEntry>())
                .OrderBy(entry => entry.Start)
        ];

        return Earliest(
            lessons.Driving.IsFromCache ? lessons.Driving.SavedAt : null,
            lessons.Theory.IsFromCache ? lessons.Theory.SavedAt : null);
    }

    private ScheduleEntry? DrivingEntry(StudentDrivingLessonItem lesson, HashSet<Guid> cancelling, DateTimeOffset now)
    {
        if (lesson is not { Slot: { Id: { } slotId, StartUtc: { } start, EndUtc: { } end } slot, Booking.Status: { } status }
            || status == DrivingBookingStatus.Cancelled)
        {
            return null;
        }

        var isCancelling = cancelling.Contains(slotId);
        var mayCancel = status == DrivingBookingStatus.Active && start > now && !isCancelling;

        return new ScheduleEntry
        {
            Kind = AppTexts.Get("Lesson.Driving"),
            Title = slot.VehicleName ?? string.Empty,
            Time = TimeOf(start, end),
            Details = DetailsOf(slot.InstructorName, slot.VehiclePlate, slot.TrainingGroundName),
            Status = status switch
            {
                DrivingBookingStatus.Completed => AppTexts.Get("Lesson.Completed"),
                DrivingBookingStatus.NoShow => AppTexts.Get("Lesson.NoShow"),
                _ when isCancelling => AppTexts.Get("Lesson.CancellationWaiting"),
                _ => null
            },
            CancelCommand = mayCancel ? new Command(async () => await CancelBookingAsync(slotId)) : null,
            Start = start
        };
    }

    private static ScheduleEntry? TheoryEntry(StudentTheoryLessonItem item)
    {
        if (item is not { Lesson: { StartUtc: { } start, EndUtc: { } end } lesson })
            return null;

        return new ScheduleEntry
        {
            Kind = AppTexts.Get("Lesson.Theory"),
            Title = lesson.TopicTitle ?? string.Empty,
            Time = TimeOf(start, end),
            Details = DetailsOf(lesson.InstructorName, lesson.GroupName),
            Status = (lesson.Status, item.IsPresent) switch
            {
                (TheoryLessonStatus.Cancelled, _) => AppTexts.Get("Lesson.Cancelled"),
                (_, true) => AppTexts.Get("Lesson.Attended"),
                (_, false) => AppTexts.Get("Lesson.Absent"),
                (TheoryLessonStatus.Completed, _) => AppTexts.Get("Lesson.Completed"),
                _ => null
            },
            Start = start
        };
    }

    private async Task CancelBookingAsync(Guid slotId)
    {
        var reason = await Shell.Current.DisplayPromptAsync(
            AppTexts.Get("Lesson.CancelTitle"),
            AppTexts.Get("Lesson.CancelReason"),
            AppTexts.Get("Lesson.CancelConfirm"),
            AppTexts.Get("Lesson.Keep"),
            maxLength: 500);
        if (string.IsNullOrWhiteSpace(reason))
            return;

        // Sent at once when the API can be reached, later otherwise; the schedule shows the cancellation waiting.
        await _synchronizer.SubmitAsync(new CancelDrivingBooking(slotId, reason.Trim()), CancellationToken.None);
        await LoadAsync();
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
