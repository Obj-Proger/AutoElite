using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using AutoElite.Mobile.Session;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// A student as their instructor sees them: how to reach them, how each of their contracts advances, without its
/// money, and, for a driving instructor, the lessons the student had or has with them, with the hours, the grade and
/// the note. It is read from the API or, without a network, from the device; the instructor writes to the student or
/// calls them from here.
/// </summary>
public sealed class StudentViewModel : ViewModel, IQueryAttributable
{
    /// <summary>The navigation parameter that carries the <see cref="StudentRequest"/>.</summary>
    public const string StudentParameter = "student";

    private readonly StudentProgressBook _book;
    private readonly UserSession _session;
    private readonly Chats _chats;
    private Guid _studentId;

    /// <summary>Initializes a new instance of the <see cref="StudentViewModel"/> class.</summary>
    /// <param name="book">The progress of the students of the instructor.</param>
    /// <param name="session">The user, whose roles tell whether they drive with the student.</param>
    /// <param name="chats">The chats, to write to the student.</param>
    public StudentViewModel(StudentProgressBook book, UserSession session, Chats chats)
    {
        _book = book;
        _session = session;
        _chats = chats;
        WriteCommand = new Command(async () => await WriteAsync());
        CallCommand = new Command(async () => await CallAsync(), () => HasPhone);
    }

    /// <summary>Gets the full name of the student.</summary>
    public string Name
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the phone and the email address of the student.</summary>
    public string Contacts
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets a value indicating whether the student gave a phone.</summary>
    public bool HasPhone
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                CallCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets the contracts of the student.</summary>
    public IReadOnlyList<StudentContractEntry> ContractEntries
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets the driving lessons of the student with the instructor, the latest first.</summary>
    public IReadOnlyList<StudentLessonEntry> Lessons
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasLessons));
        }
    } = [];

    /// <summary>Gets a value indicating whether there are lessons with the instructor to show.</summary>
    public bool HasLessons => Lessons.Count > 0;

    /// <summary>Gets that the page is from the device or could not be read, or <see langword="null"/>.</summary>
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

    /// <summary>Gets the command that opens the personal chat with the student.</summary>
    public Command WriteCommand { get; }

    /// <summary>Gets the command that calls the student.</summary>
    public Command CallCommand { get; }

    private string? Phone { get; set; }

    /// <inheritdoc />
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!query.TryGetValue(StudentParameter, out var value) || value is not StudentRequest request)
            return;

        _studentId = request.StudentId;
        Name = request.Name;
    }

    /// <summary>Reads the progress of the student and, for a driving instructor, their lessons together.</summary>
    /// <returns>A task that completes when the page shows them.</returns>
    public async Task LoadAsync()
    {
        try
        {
            var progress = await _book.ReadProgressAsync(_studentId, CancellationToken.None);
            Show(progress.Value);
            Notice = progress.IsFromCache
                ? AppTexts.Format("Student.Offline", progress.SavedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture))
                : null;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Notice = ProblemTexts.Of(exception);
        }

        try
        {
            var roles = (await _session.GetProfileAsync(CancellationToken.None)).Value.Roles ?? [];
            if (roles.Contains(UserRole.DrivingInstructor))
                Lessons = [.. (await _book.ReadLessonsAsync(_studentId, CancellationToken.None)).Value.Select(LessonOf)];
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            // The lessons stay as they were; the progress above tells whether the network is there.
        }
    }

    private static string GearboxOf(Transmission? transmission) =>
        transmission == Transmission.Automatic ? AppTexts.Get("OfferSlot.Automatic") : AppTexts.Get("OfferSlot.Manual");

    // The category of a contract, unless the name of its course names it already, such as "Category B".
    private static string? CategoryOf(StudentProgressContract contract)
    {
        if (contract.Category is not { } category)
            return null;

        var name = CategoryNames.Of(category);
        var words = (contract.CourseName ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return words.Contains(name, StringComparer.CurrentCultureIgnoreCase) ? null : name;
    }

    private static StudentContractEntry ContractOf(StudentProgressContract contract)
    {
        var culture = CultureInfo.CurrentCulture;
        var completed = contract.CompletedPracticeHours ?? 0;
        var course = contract.CoursePracticeHours ?? 0;
        var state = contract.Status switch
        {
            ContractStatus.Active => AppTexts.Get("Contracts.Active"),
            ContractStatus.Completed => AppTexts.Get("Contracts.Completed"),
            _ => AppTexts.Get("Contracts.Terminated")
        };
        var driving = contract.Driving;
        var tests = contract.Tests;

        return new StudentContractEntry
        {
            Course = LessonTexts.DetailsOf(contract.CourseName, CategoryOf(contract), GearboxOf(contract.Transmission)),
            State = LessonTexts.DetailsOf(
                state,
                contract.StartDate is { } start && contract.EndDate is { } end
                    ? string.Create(culture, $"{start.DateTime.ToString("d", culture)} – {end.DateTime.ToString("d", culture)}")
                    : null),
            Practice = AppTexts.Format("Contracts.Practice", completed.ToString("0.##", culture), course),
            PracticeProgress = course > 0 ? Math.Clamp(completed / course, 0d, 1d) : 0,
            Driving = LessonTexts.DetailsOf(
                AppTexts.Format("Student.Driving", driving?.Completed ?? 0, driving?.NoShows ?? 0, driving?.Upcoming ?? 0),
                driving?.AverageRating is { } rating ? AppTexts.Format("Student.Rating", rating.ToString("0.0", culture)) : null),
            Group = contract.Group is { } group
                ? AppTexts.Format("Student.Group", group.GroupName, group.LessonsAttended ?? 0, group.LessonsHeld ?? 0)
                : null,
            Tests = AppTexts.Format("Student.Tests", tests?.Attempts ?? 0, tests?.Passed ?? 0),
            Exams = [.. (contract.Exams ?? []).OrderBy(item => item.Exam?.ScheduledAtUtc).Select(ExamOf)]
        };
    }

    private static string ExamOf(StudentExamItem item)
    {
        var culture = CultureInfo.CurrentCulture;
        var kind = item.Exam?.Kind == ExamKind.Practice ? AppTexts.Get("Student.PracticeExam") : AppTexts.Get("Student.TheoryExam");
        var when = item.Exam?.ScheduledAtUtc?.ToLocalTime().ToString("d", culture);
        var outcome = item.Result switch
        {
            null when item.Exam?.Status == ExamStatus.Cancelled => AppTexts.Get("Student.ExamCancelled"),
            null when item.Exam?.ScheduledAtUtc < DateTimeOffset.UtcNow => AppTexts.Get("Student.ExamNoResult"),
            null => AppTexts.Get("Student.ExamComing"),
            { Passed: true } => AppTexts.Get("Student.ExamPassed"),
            _ => AppTexts.Get("Student.ExamFailed")
        };

        return LessonTexts.DetailsOf(kind, when, outcome, item.Result?.Score?.ToString(culture));
    }

    private static StudentLessonEntry LessonOf(DrivingScheduleItem item)
    {
        var culture = CultureInfo.CurrentCulture;
        var booking = item.Booking!;
        var outcome = booking.Status switch
        {
            DrivingBookingStatus.Completed => LessonTexts.DetailsOf(
                AppTexts.Format("Lesson.CompletedHours", booking.ActualHours?.ToString("0.##", culture)),
                booking.AverageGrade is { } grade ? AppTexts.Format("Student.Grade", grade.ToString("0.0", culture)) : null),
            DrivingBookingStatus.NoShow => AppTexts.Get("Lesson.StudentNoShow"),
            _ => AppTexts.Get("Student.LessonComing")
        };

        return new StudentLessonEntry
        {
            Time = item.Slot is { StartUtc: { } start, EndUtc: { } end } ? LessonTexts.TimeOf(start, end) : string.Empty,
            Outcome = outcome,
            Note = booking.InstructorNote
        };
    }

    private void Show(StudentProgress progress)
    {
        Name = string.Join(
            ' ', new[] { progress.LastName, progress.FirstName, progress.MiddleName }.Where(part => !string.IsNullOrWhiteSpace(part)));
        Phone = progress.Phone;
        HasPhone = !string.IsNullOrWhiteSpace(progress.Phone);
        Contacts = LessonTexts.DetailsOf(progress.Phone, progress.Email);
        ContractEntries = [.. (progress.Contracts ?? []).Select(ContractOf)];
    }

    private async Task CallAsync()
    {
        if (Phone is { } phone && !await PhoneCalls.CallAsync(phone))
            Notice = AppTexts.Format("Student.PhoneCopied", phone);
    }

    private async Task WriteAsync()
    {
        try
        {
            var chatId = await _chats.OpenDirectAsync(_studentId, CancellationToken.None);
            await Routes.OpenAsync(
                Routes.Chat,
                new Dictionary<string, object> { [ChatViewModel.ChatParameter] = new ChatRequest(chatId, Name, IsPersonal: true) });
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException
            or InvalidOperationException)
        {
            Notice = ProblemTexts.Of(exception);
        }
    }
}
