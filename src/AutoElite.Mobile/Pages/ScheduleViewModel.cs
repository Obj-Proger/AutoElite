using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using AutoElite.Mobile.Session;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The schedule of the signed-in user as a grid: a day, chosen in the strip of its week, or the whole week, moved by
/// the arrows or a swipe. It shows what the roles of the user have: the lessons of a student with the free slots they
/// can book, the practice slots of a driving instructor, the theory lessons of a theory instructor. A week is read
/// from the API or, without a network, from the device; tapping a lesson shows it with what the user can do with it.
/// A driving instructor offers a slot by tapping free time or the button, and repeats the slots of the week shown on
/// the next weeks; a theory instructor schedules a lesson of a group the same way.
/// </summary>
public sealed class ScheduleViewModel : ViewModel
{
    private readonly UserSession _session;
    private readonly StudentSchedule _studentSchedule;
    private readonly InstructorSchedule _instructorSchedule;
    private readonly DrivingBooking _booking;
    private readonly OfflineQueue _queue;
    private readonly OfflineSynchronizer _synchronizer;
    private readonly LessonEntries _entries;
    private readonly SlotOffers _offers;
    private readonly TimeProvider _timeProvider;
    private DateOnly? _loadedWeek;

    /// <summary>Initializes a new instance of the <see cref="ScheduleViewModel"/> class.</summary>
    /// <param name="session">The user of the application, whose roles choose what the schedule shows.</param>
    /// <param name="studentSchedule">The lessons of a student.</param>
    /// <param name="instructorSchedule">The lessons of an instructor.</param>
    /// <param name="booking">The contracts of a student and the free slots they can book.</param>
    /// <param name="queue">The actions that wait to be sent.</param>
    /// <param name="synchronizer">Sends the actions.</param>
    /// <param name="entries">Makes the cards of the lessons, with what the user can do with them.</param>
    /// <param name="offers">The slots a driving instructor offers.</param>
    /// <param name="timeProvider">The clock.</param>
    public ScheduleViewModel(
        UserSession session,
        StudentSchedule studentSchedule,
        InstructorSchedule instructorSchedule,
        DrivingBooking booking,
        OfflineQueue queue,
        OfflineSynchronizer synchronizer,
        LessonEntries entries,
        SlotOffers offers,
        TimeProvider timeProvider)
    {
        _session = session;
        _studentSchedule = studentSchedule;
        _instructorSchedule = instructorSchedule;
        _booking = booking;
        _queue = queue;
        _synchronizer = synchronizer;
        _entries = entries;
        _entries.Changed = ReloadAsync;
        _offers = offers;
        _timeProvider = timeProvider;
        SelectedDay = Today;
        PreviousCommand = new Command(async () => await MoveAsync(-1));
        NextCommand = new Command(async () => await MoveAsync(1));
        TodayCommand = new Command(async () => await ShowAsync(Today));
        ShowDayCommand = new Command(() => IsWeekView = false);
        ShowWeekCommand = new Command(() => IsWeekView = true);
        RefreshCommand = new Command(async () => await RefreshAsync());
        OpenEntryCommand = new Command<ScheduleEntry>(async entry => await OpenAsync(entry));
        PlanAtCommand = new Command<DateTimeOffset>(async start => await PlanAtAsync(start), _ => CanPlan);
        OfferCommand = new Command(async () => await OfferAsync(NextStart()));
        PlanTheoryCommand = new Command(async () => await PlanTheoryAsync(NextStart()));
        RepeatWeekCommand = new Command(async () => await RepeatWeekAsync(), () => !IsRepeating);
        Days = DaysOf([]);
    }

    /// <summary>Gets the day shown, or a day of the week shown.</summary>
    public DateOnly SelectedDay
    {
        get;
        private set
        {
            if (!SetProperty(ref field, value))
                return;

            OnPropertyChanged(nameof(GridFirstDay));
            OnPropertyChanged(nameof(Period));
            Days = DaysOf(Entries);
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    /// <summary>Gets a value indicating whether the whole week is shown rather than a day.</summary>
    public bool IsWeekView
    {
        get;
        private set
        {
            if (!SetProperty(ref field, value))
                return;

            OnPropertyChanged(nameof(IsDayView));
            OnPropertyChanged(nameof(GridFirstDay));
            OnPropertyChanged(nameof(GridDayCount));
            OnPropertyChanged(nameof(Period));
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    /// <summary>Gets a value indicating whether a day is shown.</summary>
    public bool IsDayView => !IsWeekView;

    /// <summary>Gets the first day of the grid.</summary>
    public DateOnly GridFirstDay => IsWeekView ? ScheduleWeek.FirstDayOf(SelectedDay) : SelectedDay;

    /// <summary>Gets the number of days of the grid.</summary>
    public int GridDayCount => IsWeekView ? ScheduleWeek.Days : 1;

    /// <summary>Gets the period shown, such as "Monday, 16 November" or "16 – 22 November".</summary>
    public string Period
    {
        get
        {
            var culture = CultureInfo.CurrentCulture;
            if (!IsWeekView)
            {
                var day = SelectedDay.ToString("dddd, d MMMM", culture);

                return string.Concat(day[..1].ToUpper(culture), day[1..]);
            }

            var first = ScheduleWeek.FirstDayOf(SelectedDay);
            var last = first.AddDays(ScheduleWeek.Days - 1);

            return first.Month == last.Month
                ? string.Create(culture, $"{first.Day} – {last.ToString("d MMMM", culture)}")
                : string.Create(culture, $"{first.ToString("d MMM", culture)} – {last.ToString("d MMM", culture)}");
        }
    }

    /// <summary>Gets the days of the week of the selected day, for the strip above the grid.</summary>
    public IReadOnlyList<DayChip> Days
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets the lessons of the week.</summary>
    public IReadOnlyList<ScheduleEntry> Entries
    {
        get;
        private set
        {
            if (!SetProperty(ref field, value))
                return;

            Days = DaysOf(value);
            OnPropertyChanged(nameof(IsEmpty));
        }
    } = [];

    /// <summary>Gets a value indicating whether the period shown has no lessons, once the week is read.</summary>
    public bool IsEmpty
    {
        get
        {
            if (_loadedWeek is null)
                return false;

            var first = GridFirstDay;
            var last = first.AddDays(GridDayCount);

            return !Entries.Any(entry => DayOf(entry.Start) >= first && DayOf(entry.Start) < last);
        }
    }

    /// <summary>Gets whether the week is from the device, or that it could not be read, or <see langword="null"/>.</summary>
    public string? Notice
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasNotice));
        }
    }

    /// <summary>Gets a value indicating whether there is a notice.</summary>
    public bool HasNotice => Notice is not null;

    /// <summary>Gets or sets a value indicating whether the schedule is being refreshed by pulling it down.</summary>
    public bool IsRefreshing
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>Gets the command that shows the previous day or week.</summary>
    public Command PreviousCommand { get; }

    /// <summary>Gets the command that shows the next day or week.</summary>
    public Command NextCommand { get; }

    /// <summary>Gets the command that shows today.</summary>
    public Command TodayCommand { get; }

    /// <summary>Gets the command that shows a day.</summary>
    public Command ShowDayCommand { get; }

    /// <summary>Gets the command that shows the whole week.</summary>
    public Command ShowWeekCommand { get; }

    /// <summary>Gets the command that sends the waiting actions and reads the week again.</summary>
    public Command RefreshCommand { get; }

    /// <summary>Gets the command that shows a lesson with what the user can do with it.</summary>
    public Command<ScheduleEntry> OpenEntryCommand { get; }

    /// <summary>Gets a value indicating whether the user is a driving instructor, who offers slots.</summary>
    public bool IsDrivingInstructor
    {
        get;
        private set
        {
            if (!SetProperty(ref field, value))
                return;

            OnPropertyChanged(nameof(CanPlan));
            PlanAtCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets a value indicating whether the user is a theory instructor, who schedules lessons of their groups.</summary>
    public bool IsTheoryInstructor
    {
        get;
        private set
        {
            if (!SetProperty(ref field, value))
                return;

            OnPropertyChanged(nameof(CanPlan));
            PlanAtCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets a value indicating whether the user plans lessons or slots on the schedule.</summary>
    public bool CanPlan => IsDrivingInstructor || IsTheoryInstructor;

    /// <summary>Gets a value indicating whether the slots of the week are being repeated.</summary>
    public bool IsRepeating
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                RepeatWeekCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets the command that offers a slot or schedules a lesson at the free time tapped on the grid.</summary>
    public Command<DateTimeOffset> PlanAtCommand { get; }

    /// <summary>Gets the command that schedules a theory lesson on the day shown, at the next full hour.</summary>
    public Command PlanTheoryCommand { get; }

    /// <summary>Gets the command that offers a slot on the day shown, at the next full hour.</summary>
    public Command OfferCommand { get; }

    /// <summary>Gets the command that repeats the slots of the week shown on the next weeks.</summary>
    public Command RepeatWeekCommand { get; }

    private DateOnly Today => DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);

    /// <summary>Reads the week of the selected day again, each time the page appears, since lessons may have changed.</summary>
    /// <returns>A task that completes when the week is shown.</returns>
    public Task AppearAsync() => ReloadAsync();

    // Reads the week of the selected day, unless it is already shown, when the user moves to another day.
    private Task LoadAsync() =>
        _loadedWeek == ScheduleWeek.FirstDayOf(SelectedDay) ? Task.CompletedTask : ReloadAsync();

    private static DateOnly DayOf(DateTimeOffset moment) => DateOnly.FromDateTime(moment.ToLocalTime().DateTime);

    private static DateTimeOffset? Earliest(DateTimeOffset? first, DateTimeOffset? second) =>
        first is { } a && second is { } b ? (a < b ? a : b) : first ?? second;

    private List<DayChip> DaysOf(IReadOnlyList<ScheduleEntry> entries)
    {
        var culture = CultureInfo.CurrentCulture;
        var first = ScheduleWeek.FirstDayOf(SelectedDay);
        var busy = entries.Select(entry => DayOf(entry.Start)).ToHashSet();

        return
        [
            .. Enumerable.Range(0, ScheduleWeek.Days).Select(offset => first.AddDays(offset)).Select(day => new DayChip
            {
                Date = day,
                Name = day.ToString("ddd", culture),
                Number = day.Day.ToString(culture),
                IsSelected = day == SelectedDay,
                IsToday = day == Today,
                HasLessons = busy.Contains(day),
                SelectCommand = new Command(async () => await ShowAsync(day))
            })
        ];
    }

    private Task MoveAsync(int direction) => ShowAsync(SelectedDay.AddDays(direction * GridDayCount));

    private async Task ShowAsync(DateOnly day)
    {
        SelectedDay = day;
        await LoadAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            await _synchronizer.SynchronizeAsync(CancellationToken.None);
            await ReloadAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    // Reads the week of the selected day; when the user has moved to another week meanwhile, that one is read instead.
    private async Task ReloadAsync()
    {
        var week = ScheduleWeek.FirstDayOf(SelectedDay);
        var roles = new HashSet<UserRole>();
        try
        {
            roles = [.. ((await _session.GetProfileAsync(CancellationToken.None)).Value.Roles ?? []).OfType<UserRole>()];
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            // Without the profile the schedule cannot tell what to show; it is read again the next time.
        }

        var waiting = (await _queue.ListAsync(CancellationToken.None))
            .Where(action => !action.IsRejected)
            .Select(action => LessonEntries.SubjectOf(action.Action))
            .OfType<Guid>()
            .ToHashSet();
        var entries = new List<ScheduleEntry>();
        DateTimeOffset? offlineSince = null;
        var isIncomplete = roles.Count == 0;
        try
        {
            if (roles.Contains(UserRole.Student))
                offlineSince = Earliest(offlineSince, await ReadStudentWeekAsync(week, entries, waiting));

            if (roles.Contains(UserRole.DrivingInstructor))
            {
                var slots = await _instructorSchedule.ReadDrivingWeekAsync(week, CancellationToken.None);
                entries.AddRange(slots.Value.Select(slot => _entries.DrivingSlot(slot, waiting)).OfType<ScheduleEntry>());
                offlineSince = Earliest(offlineSince, slots.IsFromCache ? slots.SavedAt : null);
            }

            if (roles.Contains(UserRole.TheoryInstructor))
            {
                var lessons = await _instructorSchedule.ReadTheoryWeekAsync(week, CancellationToken.None);
                entries.AddRange(lessons.Value.Select(lesson => _entries.TheoryLesson(lesson, waiting)).OfType<ScheduleEntry>());
                offlineSince = Earliest(offlineSince, lessons.IsFromCache ? lessons.SavedAt : null);
            }
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            // A week never read on this device and no network: it is shown once it can be read.
            isIncomplete = true;
        }

        if (week != ScheduleWeek.FirstDayOf(SelectedDay))
        {
            await ReloadAsync();

            return;
        }

        _loadedWeek = week;
        IsDrivingInstructor = roles.Contains(UserRole.DrivingInstructor);
        IsTheoryInstructor = roles.Contains(UserRole.TheoryInstructor);
        Entries = [.. entries.OrderBy(entry => entry.Start)];
        Notice = isIncomplete
            ? AppTexts.Get("Schedule.Unavailable")
            : offlineSince is { } savedAt
                ? AppTexts.Format("Schedule.Offline", savedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture))
                : null;
    }

    // Adds the lessons of a student and the free slots their contracts can book; gives when they were read, if they
    // come from the device.
    private async Task<DateTimeOffset?> ReadStudentWeekAsync(DateOnly week, List<ScheduleEntry> entries, HashSet<Guid> waiting)
    {
        var lessons = await _studentSchedule.ReadWeekAsync(week, CancellationToken.None);
        entries.AddRange(lessons.Driving.Value.Select(lesson => _entries.StudentDriving(lesson, waiting)).OfType<ScheduleEntry>());
        entries.AddRange(lessons.Theory.Value.Select(LessonEntries.StudentTheory).OfType<ScheduleEntry>());

        var contracts = await _booking.ReadActiveContractsAsync(CancellationToken.None);
        var free = new Dictionary<Guid, (DrivingSlotItem Slot, List<ContractListItem> Contracts)>();
        foreach (var contract in contracts.Value)
        {
            if (contract.Id is not { } contractId)
                continue;

            foreach (var slot in await _booking.ReadAvailableWeekAsync(contractId, week, CancellationToken.None))
            {
                if (slot.Id is not { } slotId)
                    continue;

                if (!free.TryGetValue(slotId, out var offer))
                    free[slotId] = offer = (slot, []);

                offer.Contracts.Add(contract);
            }
        }

        entries.AddRange(free.Values.Select(offer => _entries.FreeSlot(offer.Slot, offer.Contracts, waiting)).OfType<ScheduleEntry>());

        return Earliest(
            lessons.Driving.IsFromCache ? lessons.Driving.SavedAt : null,
            lessons.Theory.IsFromCache ? lessons.Theory.SavedAt : null);
    }

    // The next full hour on the day shown, or the morning of a later day; the page of the slot keeps it in the future.
    private DateTimeOffset NextStart()
    {
        var now = _timeProvider.GetLocalNow();
        var start = SelectedDay > Today
            ? SelectedDay.ToDateTime(new TimeOnly(9, 0))
            : now.DateTime.Date.AddHours(now.Hour + 1);

        return new DateTimeOffset(start);
    }

    // A slot for a driving instructor, a lesson for a theory instructor; a user who is both chooses.
    private async Task PlanAtAsync(DateTimeOffset start)
    {
        if (!IsTheoryInstructor)
        {
            await OfferAsync(start);

            return;
        }

        if (!IsDrivingInstructor)
        {
            await PlanTheoryAsync(start);

            return;
        }

        var slot = AppTexts.Get("Schedule.Offer");
        var lesson = AppTexts.Get("Schedule.PlanTheory");
        var choice = await Shell.Current.DisplayActionSheetAsync(null, AppTexts.Get("Common.Cancel"), null, slot, lesson);
        if (choice == slot)
            await OfferAsync(start);
        else if (choice == lesson)
            await PlanTheoryAsync(start);
    }

    private static Task PlanTheoryAsync(DateTimeOffset start) =>
        Routes.OpenAsync(
            Routes.PlanTheory, new Dictionary<string, object> { [PlanTheoryViewModel.LessonParameter] = new TheoryLessonRequest(start) });

    private static Task OfferAsync(DateTimeOffset start) =>
        Routes.OpenAsync(Routes.OfferSlot, new Dictionary<string, object> { [OfferSlotViewModel.SlotParameter] = new SlotRequest(start) });

    // Asks for how many weeks, repeats the slots of the week shown on them and tells how many were offered.
    private async Task RepeatWeekAsync()
    {
        var week = ScheduleWeek.FirstDayOf(SelectedDay);
        string[] choices = [.. Enumerable.Range(1, 4).Select(weeks => AppTexts.Format("Schedule.RepeatWeeks", weeks))];
        var choice = await Shell.Current.DisplayActionSheetAsync(
            AppTexts.Get("Schedule.RepeatTitle"), AppTexts.Get("Common.Cancel"), null, choices);
        var weeksAhead = Array.IndexOf(choices, choice) + 1;
        if (weeksAhead <= 0)
            return;

        IsRepeating = true;
        string message;
        try
        {
            var (offered, skipped) = await _offers.RepeatWeekAsync(week, weeksAhead, CancellationToken.None);
            message = skipped == 0
                ? AppTexts.Format("Schedule.Repeated", offered)
                : AppTexts.Format("Schedule.RepeatedSkipped", offered, skipped);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            message = ProblemTexts.Of(exception);
        }
        finally
        {
            IsRepeating = false;
        }

        await Shell.Current.DisplayAlertAsync(AppTexts.Get("Schedule.RepeatTitle"), message, AppTexts.Get("Common.Ok"));
    }

    // Shows a lesson with its details and what the user can do with it, and does what they choose.
    private static async Task OpenAsync(ScheduleEntry entry)
    {
        var details = string.Join(
            Environment.NewLine,
            new[] { entry.Time, entry.Details, entry.Status }.Where(line => !string.IsNullOrWhiteSpace(line)));
        string[] choices = [.. entry.Actions.Select(action => action.Text)];
        var choice = await Shell.Current.DisplayActionSheetAsync(
            $"{entry.Kind}: {entry.Title}{Environment.NewLine}{details}", AppTexts.Get("Schedule.Close"), null, choices);
        var chosen = entry.Actions.FirstOrDefault(action => action.Text == choice);
        if (chosen?.Command.CanExecute(null) == true)
            chosen.Command.Execute(null);
    }
}
