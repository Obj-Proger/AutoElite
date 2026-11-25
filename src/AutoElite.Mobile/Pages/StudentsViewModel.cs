using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Controls;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using AutoElite.Mobile.Session;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The students tab. A driving instructor sees their students, those they had or have lessons with in the last weeks
/// and the next two, the nearest lesson first, filtered by a part of the name; each opens the student as their
/// instructor sees them. A manager or an administrator finds any student of the school by a part of the name, the email
/// address or the phone number, each opening the card with the contracts and payments. A user with both roles sees
/// both. The students of an instructor are read from the API or, without a network, from the device.
/// </summary>
public sealed class StudentsViewModel : ViewModel
{
    private readonly StudentProgressBook _book;
    private readonly UserSession _session;
    private readonly TimeProvider _timeProvider;
    private IReadOnlyList<DrivingStudent> _students = [];

    /// <summary>Initializes a new instance of the <see cref="StudentsViewModel"/> class.</summary>
    /// <param name="search">The search of the students.</param>
    /// <param name="book">The students of a driving instructor.</param>
    /// <param name="session">The user, whose roles choose what the tab shows.</param>
    /// <param name="timeProvider">The clock.</param>
    public StudentsViewModel(StudentSearch search, StudentProgressBook book, UserSession session, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(search);
        _book = book;
        _session = session;
        _timeProvider = timeProvider;
        Search = search;
        Search.Chosen = OpenCardAsync;
        RefreshCommand = new Command(async () => await RefreshAsync());
    }

    /// <summary>Gets the search of the students.</summary>
    public StudentSearch Search { get; }

    /// <summary>Gets a value indicating whether the user is a driving instructor, who sees their students.</summary>
    public bool IsDriver
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets a value indicating whether the user works in the office, who finds any student.</summary>
    public bool IsOffice
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets a value indicating whether the user has both roles, and both parts get their headings.</summary>
    public bool HasBoth => IsDriver && IsOffice;

    /// <summary>Gets or sets a part of the name that filters the students of the instructor.</summary>
    public string Filter
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
                ShowStudents();
        }
    } = string.Empty;

    /// <summary>Gets the students of the instructor that the filter keeps.</summary>
    public IReadOnlyList<LessonRow> Students
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets what the list of the instructor says when it is empty, or <see langword="null"/>.</summary>
    public string? EmptyText
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets that the students are from the device, or why they could not be read.</summary>
    public string? Notice
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets or sets a value indicating whether the page is pulled down to read the students again.</summary>
    public bool IsRefreshing
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>Gets the command that reads the students again.</summary>
    public Command RefreshCommand { get; }

    /// <summary>Shows what the roles of the user call for: their students, the search, or both.</summary>
    /// <returns>A task that completes when they are shown.</returns>
    public async Task LoadAsync()
    {
        try
        {
            var profile = (await _session.GetProfileAsync(CancellationToken.None)).Value;
            var roles = profile.Roles ?? [];
            IsDriver = roles.Contains(UserRole.PracticeInstructor);
            IsOffice = roles.Contains(UserRole.Manager) || roles.Contains(UserRole.Administrator);
            OnPropertyChanged(nameof(HasBoth));
            if (IsDriver && profile.Id is { } instructorId)
                await ReadStudentsAsync(instructorId);

            if (IsOffice)
                await Search.SearchNowAsync();
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Notice = ProblemTexts.Of(exception);
        }
    }

    private static Task OpenCardAsync(Guid studentId, string name) =>
        Routes.OpenAsync(
            Routes.ManagedStudent,
            new Dictionary<string, object> { [ManagedStudentViewModel.StudentParameter] = new StudentRequest(studentId, name) });

    private static Task OpenStudentAsync(DrivingStudent student) =>
        Routes.OpenAsync(
            Routes.Student,
            new Dictionary<string, object>
            {
                [StudentViewModel.StudentParameter] = new StudentRequest(student.StudentId, student.Name)
            });

    private async Task ReadStudentsAsync(Guid instructorId)
    {
        var students = await _book.ReadStudentsAsync(instructorId, CancellationToken.None);
        _students = students.Value;
        Notice = students.IsFromCache
            ? AppTexts.Format("Students.Offline", students.SavedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture))
            : null;
        ShowStudents();
    }

    private void ShowStudents()
    {
        var now = _timeProvider.GetUtcNow();
        var filter = Filter.Trim();
        Students =
        [
            .. _students
                .Where(student => filter.Length == 0 || student.Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase))
                .Select(student => RowOf(student, now))
        ];
        EmptyText = Students.Count > 0
            ? null
            : filter.Length > 0 ? AppTexts.Get("Students.NotFound") : AppTexts.Get("Students.NoneOfMine");
    }

    private static LessonRow RowOf(DrivingStudent student, DateTimeOffset now)
    {
        var culture = CultureInfo.CurrentCulture;
        var last = student.Last is { } lastStart
            ? AppTexts.Format("Students.Last", lastStart.ToLocalTime().ToString("d MMM", culture))
            : null;

        return new LessonRow
        {
            Icon = "car",
            Tone = student.Next is null ? StatusTone.Neutral : StatusTone.Accent,
            Title = student.Name,
            Subtitle = LessonTexts.DetailsOf(AppTexts.Format("Students.Done", student.LessonsDone), last),
            Time = student.Next?.ToLocalTime().ToString("t", culture) ?? string.Empty,
            Day = student.Next is { } next ? TodayCards.DayWordOf(next, now) : AppTexts.Get("Students.NoNext"),
            Command = new Command(async () => await OpenStudentAsync(student))
        };
    }

    private async Task RefreshAsync()
    {
        try
        {
            await LoadAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }
}
