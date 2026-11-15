using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using AutoElite.Mobile.Session;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// An exam an instructor sees: when and for which group, and each current member of the group with their result. Once
/// a practice exam has begun, a driving instructor records the result of a member; a recorded practice result is
/// corrected by a driving instructor and a theory result by the instructor responsible for the group. It is read from
/// the API or, without a network, from the device.
/// </summary>
public sealed class ExamViewModel : ViewModel, IQueryAttributable
{
    /// <summary>The navigation parameter that carries the identifier of the exam.</summary>
    public const string ExamParameter = "exam";

    private readonly ExamBook _exams;
    private readonly UserSession _session;
    private readonly TimeProvider _timeProvider;
    private Guid _examId;

    /// <summary>Initializes a new instance of the <see cref="ExamViewModel"/> class.</summary>
    /// <param name="exams">The exams.</param>
    /// <param name="session">The user, whose roles tell what they may record.</param>
    /// <param name="timeProvider">The clock.</param>
    public ExamViewModel(ExamBook exams, UserSession session, TimeProvider timeProvider)
    {
        _exams = exams;
        _session = session;
        _timeProvider = timeProvider;
    }

    /// <summary>Gets the title of the exam.</summary>
    public string Title
    {
        get;
        private set => SetProperty(ref field, value);
    } = AppTexts.Get("Exam.Kind");

    /// <summary>Gets when the exam is and for which group.</summary>
    public string Details
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets that the exam is cancelled or coming, or <see langword="null"/>.</summary>
    public string? State
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasState));
        }
    }

    /// <summary>Gets a value indicating whether there is a state to show.</summary>
    public bool HasState => State is not null;

    /// <summary>Gets the members of the group with their results.</summary>
    public IReadOnlyList<ExamMemberEntry> Members
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets that the exam is from the device or could not be read, or <see langword="null"/>.</summary>
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

    /// <inheritdoc />
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue(ExamParameter, out var value) && value is Guid examId)
            _examId = examId;
    }

    /// <summary>Reads the exam, each time the page appears, so that a result just recorded shows.</summary>
    /// <returns>A task that completes when the page shows it.</returns>
    public async Task LoadAsync()
    {
        try
        {
            var profile = (await _session.GetProfileAsync(CancellationToken.None)).Value;
            var details = await _exams.ReadAsync(_examId, CancellationToken.None);
            Show(details.Value, profile);
            Notice = details.IsFromCache
                ? AppTexts.Format("Exam.Offline", details.SavedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture))
                : null;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Notice = ProblemTexts.Of(exception);
        }
    }

    private static Command OpenResult(ExamResultRequest request) =>
        new(async () => await Routes.OpenAsync(
            Routes.ExamResult, new Dictionary<string, object> { [ExamResultViewModel.ResultParameter] = request }));

    private void Show(ExamDetails details, UserProfile profile)
    {
        if (details.Exam is not { ScheduledAtUtc: { } scheduledAt } exam)
            return;

        var roles = (profile.Roles ?? []).OfType<UserRole>().ToHashSet();
        var isPractice = exam.Kind == ExamKind.Practice;
        var isCancelled = exam.Status == ExamStatus.Cancelled;
        var hasBegun = scheduledAt <= _timeProvider.GetUtcNow();
        var mayRecord = isPractice && !isCancelled && hasBegun && roles.Contains(UserRole.DrivingInstructor);
        var mayCorrect = isPractice
            ? roles.Contains(UserRole.DrivingInstructor)
            : roles.Contains(UserRole.TheoryInstructor) && exam.ResponsibleInstructorId == profile.Id;

        Title = ExamTexts.TitleOf(exam);
        Details = LessonTexts.DetailsOf(scheduledAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture), exam.GroupName);
        State = isCancelled
            ? LessonTexts.DetailsOf(AppTexts.Get("Lesson.Cancelled"), exam.CancellationReason)
            : !hasBegun ? AppTexts.Get("Exam.NotBegun")
            : !isPractice ? AppTexts.Get("Exam.TheoryResults")
            : null;

        var results = (details.Results ?? [])
            .Where(result => result.ContractId is not null)
            .ToDictionary(result => result.ContractId!.Value);
        var members = (details.Members ?? [])
            .Where(member => member.ContractId is not null)
            .Select(member => (ContractId: member.ContractId!.Value, Name: member.StudentName, Number: member.ContractNumber))
            .ToList();

        // A student who left the group after their result keeps it on the exam.
        members.AddRange(results.Values
            .Where(result => members.All(member => member.ContractId != result.ContractId))
            .Select(result => (ContractId: result.ContractId!.Value, Name: result.StudentName, Number: result.ContractNumber)));

        Members =
        [
            .. members.Select(member =>
            {
                var (contractId, studentName, number) = member;
                var name = LessonTexts.DetailsOf(studentName, number);
                var title = LessonTexts.DetailsOf(Title, studentName);
                if (results.TryGetValue(contractId, out var result))
                {
                    return new ExamMemberEntry
                    {
                        Name = name,
                        Result = ExamTexts.ResultOf(result),
                        Comment = LessonTexts.DetailsOf(
                            result.Comment, result.CorrectedAtUtc is null ? null : AppTexts.Get("Exam.Corrected")),
                        ActionText = mayCorrect && !isCancelled ? AppTexts.Get("Exam.Correct") : null,
                        Command = mayCorrect && !isCancelled
                            ? OpenResult(new ExamResultRequest(_examId, contractId, title, true, result.Passed, result.Score))
                            : null
                    };
                }

                return new ExamMemberEntry
                {
                    Name = name,
                    Result = AppTexts.Get("Exam.NoResult"),
                    ActionText = mayRecord ? AppTexts.Get("Exam.Record") : null,
                    Command = mayRecord ? OpenResult(new ExamResultRequest(_examId, contractId, title, false)) : null
                };
            })
        ];
    }
}
