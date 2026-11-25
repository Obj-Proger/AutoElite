using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;
using AutoElite.Client.Realtime;
using AutoElite.Mobile.Controls;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using AutoElite.Mobile.Session;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// Today, the first tab of the signed-in user: the lesson going on or the next one in a highlighted card, with what the
/// user can do with it; the figures of their role (the hours driven and the debt of a student, the lessons of the day
/// and the free slots of an instructor, the leads of a manager); the tasks of the staff, and for the office the payments
/// that wait for their money and the students who owe money; the next lessons as rows
/// (HomeViewModel.Lessons.cs); and whether the data is from the device, with the actions that wait to be sent and those
/// the school did not accept. The bell above the tabs counts the notifications; the other sections are tabs.
/// </summary>
public sealed partial class HomeViewModel : ViewModel
{
    private readonly UserSession _session;
    private readonly StudentSchedule _schedule;
    private readonly InstructorSchedule _instructorSchedule;
    private readonly RealtimeConnection _realtime;
    private readonly OfflineQueue _queue;
    private readonly OfflineSynchronizer _synchronizer;
    private readonly TimeProvider _timeProvider;
    private readonly LessonEntries _entries;
    private readonly StaffTasks _tasks;
    private readonly LeadBook _leads;
    private readonly StudentContracts _contracts;
    private readonly OfficeMoney _money;

    /// <summary>Initializes a new instance of the <see cref="HomeViewModel"/> class.</summary>
    /// <param name="session">The user of the application.</param>
    /// <param name="schedule">The lessons of the student.</param>
    /// <param name="instructorSchedule">The lessons of the instructor.</param>
    /// <param name="realtime">The connection to the hub of the API, started when the page appears.</param>
    /// <param name="queue">The actions that wait to be sent.</param>
    /// <param name="synchronizer">Sends the actions.</param>
    /// <param name="timeProvider">The clock.</param>
    /// <param name="entries">Makes the cards of the lessons, with what the user can do with them.</param>
    /// <param name="tasks">The tasks the school gives a member of its staff.</param>
    /// <param name="leads">The leads of the school, for a manager.</param>
    /// <param name="contracts">The contracts of a student, for their hours and debt.</param>
    /// <param name="money">The payments and debts the office follows.</param>
    public HomeViewModel(
        UserSession session,
        StudentSchedule schedule,
        InstructorSchedule instructorSchedule,
        RealtimeConnection realtime,
        OfflineQueue queue,
        OfflineSynchronizer synchronizer,
        TimeProvider timeProvider,
        LessonEntries entries,
        StaffTasks tasks,
        LeadBook leads,
        StudentContracts contracts,
        OfficeMoney money)
    {
        _session = session;
        _schedule = schedule;
        _instructorSchedule = instructorSchedule;
        _realtime = realtime;
        _queue = queue;
        _synchronizer = synchronizer;
        _timeProvider = timeProvider;
        _entries = entries;
        _entries.Changed = LoadAsync;
        _tasks = tasks;
        _leads = leads;
        _contracts = contracts;
        _money = money;
        OpenTasksCommand = new Command(async () => await Routes.OpenAsync(Routes.Tasks));
        OpenLeadsCommand = new Command(async () => await Routes.GoToAsync(Routes.Leads));
        OpenWaitingCommand = new Command(async () => await Routes.OpenAsync(Routes.Money));
        OpenDebtsCommand = new Command(async () => await Routes.OpenAsync(
            Routes.Money, new Dictionary<string, object> { [MoneyViewModel.ViewParameter] = MoneyViewModel.DebtsView }));
        RefreshCommand = new Command(async () => await RefreshAsync());
        DismissRefusalsCommand = new Command(async () => await DismissRefusalsAsync());
        BookCommand = new Command(async () => await Routes.OpenAsync(Routes.Booking));
        OpenScheduleCommand = new Command(async () => await Routes.GoToAsync(Routes.Schedule));
        OpenContractsCommand = new Command(async () => await Routes.OpenAsync(Routes.Contracts));
    }

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

    /// <summary>Gets a value indicating whether the user is a theory instructor, who marks attendance at their lessons.</summary>
    public bool IsTheoryInstructor
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnRolesChanged();
        }
    }

    /// <summary>Gets a value indicating whether the page shows lessons, which it does for every user of the application.</summary>
    public bool ShowsLessons => IsStudent || IsDrivingInstructor || IsTheoryInstructor;

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

    /// <summary>Gets the lesson going on or the next one, or <see langword="null"/> when there is none.</summary>
    public NextLessonCard? Next
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasNext));
        }
    }

    /// <summary>Gets a value indicating whether there is a lesson going on or ahead.</summary>
    public bool HasNext => Next is not null;

    /// <summary>Gets the lessons after the next one, a few of them; the schedule has the rest.</summary>
    public IReadOnlyList<LessonRow> Rows
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasRows));
        }
    } = [];

    /// <summary>Gets a value indicating whether there are lessons after the next one.</summary>
    public bool HasRows => Rows.Count > 0;

    /// <summary>Gets the command of an empty schedule: booking a driving lesson for a student, nothing for others.</summary>
    public Command? EmptyCommand => IsStudent ? BookCommand : null;

    /// <summary>Gets a value indicating whether the user is an instructor, who sees the lessons of the day.</summary>
    public bool IsInstructor => IsDrivingInstructor || IsTheoryInstructor;

    /// <summary>Gets how many lessons of the day are held.</summary>
    public string LessonsHeld
    {
        get;
        private set => SetProperty(ref field, value);
    } = "0";

    /// <summary>Gets how many lessons the day has, after the number held.</summary>
    public string LessonsOfDay
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the part of the lessons of the day that is held, from 0 to 1.</summary>
    public double DayProgress
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets how many free slots of a driving instructor wait for a booking in the next seven days.</summary>
    public string FreeSlots
    {
        get;
        private set => SetProperty(ref field, value);
    } = "0";

    /// <summary>Gets the hours a student has driven on their active contract.</summary>
    public string DrivingHours
    {
        get;
        private set => SetProperty(ref field, value);
    } = "0";

    /// <summary>Gets the hours of driving of the course, after the hours driven.</summary>
    public string CourseHours
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the part of the driving of the course a student has done, from 0 to 1.</summary>
    public double DrivingProgress
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets how many theory lessons a student has attended.</summary>
    public string TheoryAttended
    {
        get;
        private set => SetProperty(ref field, value);
    } = "0";

    /// <summary>Gets a value indicating whether a student has an active contract, whose progress the page shows.</summary>
    public bool HasContract
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets what a student owes on their active contract, as the title of its row.</summary>
    public string Debt
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the contract the debt is on.</summary>
    public string DebtContract
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets a value indicating whether a student owes money on their active contract.</summary>
    public bool HasDebt
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets the command that opens the contracts and payments of a student.</summary>
    public Command OpenContractsCommand { get; }

    /// <summary>Gets the command that opens the schedule.</summary>
    public Command OpenScheduleCommand { get; }

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

    /// <summary>Gets a value indicating whether the user is a member of the staff, who has tasks.</summary>
    public bool IsStaff
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets how many tasks are open and overdue.</summary>
    public string TasksSummary
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets how many tasks are open.</summary>
    public string TasksCount
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the tone of the tasks: danger when some are overdue.</summary>
    public StatusTone TasksTone
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets the command that opens the tasks.</summary>
    public Command OpenTasksCommand { get; }

    /// <summary>Gets a value indicating whether the user is a manager or an administrator, who works on the leads.</summary>
    public bool IsManager
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets how many leads are new.</summary>
    public string NewLeads
    {
        get;
        private set => SetProperty(ref field, value);
    } = "0";

    /// <summary>Gets how many leads the user is working on.</summary>
    public string MyLeads
    {
        get;
        private set => SetProperty(ref field, value);
    } = "0";

    /// <summary>Gets the limit of the leads the user may work on, after their number.</summary>
    public string LeadLimit
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the part of their limit the leads of the user take, from 0 to 1.</summary>
    public double LeadsProgress
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets the command that opens the leads.</summary>
    public Command OpenLeadsCommand { get; }

    /// <summary>Gets what the payments that wait for their confirmation come to.</summary>
    public string WaitingSummary
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets how many payments wait for their confirmation.</summary>
    public string WaitingCount
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets how much the students owe.</summary>
    public string DebtsSummary
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets how many contracts have a debt.</summary>
    public string DebtsCount
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the tone of the debts: danger when there are some.</summary>
    public StatusTone DebtsTone
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets the command that opens the payments that wait for their confirmation.</summary>
    public Command OpenWaitingCommand { get; }

    /// <summary>Gets the command that opens the students who owe money.</summary>
    public Command OpenDebtsCommand { get; }

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
        DateTimeOffset? offlineSince = null;
        try
        {
            var profile = await _session.GetProfileAsync(CancellationToken.None);
            IsStudent = profile.Value.Roles?.Contains(UserRole.Student) == true;
            IsDrivingInstructor = profile.Value.Roles?.Contains(UserRole.DrivingInstructor) == true;
            IsTheoryInstructor = profile.Value.Roles?.Contains(UserRole.TheoryInstructor) == true;
            IsStaff = profile.Value.Roles?.Any(role => role is UserRole.Administrator or UserRole.Manager
                or UserRole.DrivingInstructor or UserRole.TheoryInstructor) == true;
            IsManager = profile.Value.Roles?.Any(role => role is UserRole.Administrator or UserRole.Manager) == true;
            if (profile.IsFromCache)
                offlineSince = profile.SavedAt;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            // Neither the API nor the device has the profile; the page shows what it can read without it.
        }

        var actions = await _queue.ListAsync(CancellationToken.None);
        var waiting = actions.Where(action => !action.IsRejected)
            .Select(action => LessonEntries.SubjectOf(action.Action))
            .OfType<Guid>()
            .ToHashSet();
        var lessons = new List<ScheduleEntry>();
        if (IsStudent)
            offlineSince = Earliest(offlineSince, await LoadStudentLessonsAsync(lessons, waiting));

        if (IsDrivingInstructor)
            offlineSince = Earliest(offlineSince, await LoadInstructorLessonsAsync(lessons, waiting));

        if (IsTheoryInstructor)
            offlineSince = Earliest(offlineSince, await LoadTheoryInstructorLessonsAsync(lessons, waiting));

        Lessons = [.. lessons.OrderBy(entry => entry.Start)];
        ShowLessons();

        Refusals = [.. actions.Where(action => action.IsRejected).Select(PendingActionTexts.DescribeRefusal)];

        var lines = new List<string>();
        if (offlineSince is { } savedAt)
            lines.Add(AppTexts.Format("Home.Offline", savedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)));

        var waitingCount = actions.Count(action => !action.IsRejected);
        if (waitingCount > 0)
            lines.Add(AppTexts.Format("Home.Pending", waitingCount));

        Status = lines.Count > 0 ? string.Join(Environment.NewLine, lines) : null;
        if (IsStudent)
            await ShowContractAsync();

        if (IsStaff)
            await CountTasksAsync();

        if (IsManager)
        {
            await CountLeadsAsync();
            await CountMoneyAsync();
        }

        await ConnectAsync();
    }

    private static DateTimeOffset? Earliest(DateTimeOffset? first, DateTimeOffset? second) =>
        first is { } a && second is { } b ? (a < b ? a : b) : first ?? second;

    // Shows how many tasks are open and how many of them are overdue; without a network the last numbers stay.
    private async Task CountTasksAsync()
    {
        try
        {
            var (open, _) = await _tasks.ReadOpenAsync(CancellationToken.None);
            var now = _timeProvider.GetUtcNow();
            var overdue = open.Count(task => task.DueAtUtc < now);
            TasksCount = open.Count.ToString(CultureInfo.CurrentCulture);
            TasksTone = overdue > 0 ? StatusTone.Danger : StatusTone.Neutral;
            TasksSummary = open.Count == 0
                ? AppTexts.Get("Home.NoTasks")
                : overdue > 0 ? AppTexts.Format("Home.TasksOverdue", open.Count, overdue) : AppTexts.Format("Home.Tasks", open.Count);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            // The numbers are shown once the tasks can be read.
        }
    }

    // Shows how many leads are new and how many the user is working on of the number their limit allows; without a network
    // the last numbers stay.
    private async Task CountLeadsAsync()
    {
        try
        {
            var fresh = (await _leads.ReadNewAsync(CancellationToken.None)).Value.Count;
            var workload = (await _leads.ReadWorkloadAsync(CancellationToken.None)).Value;
            var mine = workload.InProgress ?? 0;
            var limit = workload.Limit ?? 0;
            NewLeads = fresh.ToString(CultureInfo.CurrentCulture);
            MyLeads = mine.ToString(CultureInfo.CurrentCulture);
            LeadLimit = AppTexts.Format("Today.Of", limit);
            LeadsProgress = limit > 0 ? Math.Clamp((double)mine / limit, 0, 1) : 0;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            // The numbers are shown once the leads can be read.
        }
    }

    // Shows how many payments wait for their money and how many students owe money; without a network the last numbers
    // stay.
    private async Task CountMoneyAsync()
    {
        try
        {
            var waiting = (await _money.ReadWaitingAsync(CancellationToken.None)).Value;
            var debts = (await _money.ReadDebtsAsync(CancellationToken.None)).Value
                .Where(contract => contract.DebtAmount > 0)
                .ToList();
            WaitingCount = waiting.Count.ToString(CultureInfo.CurrentCulture);
            WaitingSummary = waiting.Count == 0
                ? AppTexts.Get("Home.NoWaiting")
                : MoneyViewModel.TotalOf(waiting.Select(payment => (payment.Currency, payment.Amount ?? 0)));
            DebtsCount = debts.Count.ToString(CultureInfo.CurrentCulture);
            DebtsSummary = debts.Count == 0
                ? AppTexts.Get("Home.NoDebts")
                : MoneyViewModel.TotalOf(debts.Select(contract => (contract.Currency, contract.DebtAmount ?? 0)));
            DebtsTone = debts.Count > 0 ? StatusTone.Danger : StatusTone.Neutral;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            // The numbers are shown once the money can be read.
        }
    }

    // Connects to the hub, so that new notifications and messages update the counts above the tabs at once; without a
    // network it is tried again the next time the page appears, and a lost connection reconnects by itself.
    private async Task ConnectAsync()
    {
        try
        {
            await _realtime.StartAsync(CancellationToken.None);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // Updates arrive when the page is next shown with a network.
        }
    }

    private void OnRolesChanged()
    {
        OnPropertyChanged(nameof(ShowsLessons));
        OnPropertyChanged(nameof(HasNoLessons));
        OnPropertyChanged(nameof(IsInstructor));
        OnPropertyChanged(nameof(EmptyCommand));
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
}
