using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The tests of a student: their exams, a theory exam to take once it has begun; the active templates of the category
/// of their first active contract, each to start a practice attempt from; and their practice attempts, the one in
/// progress to continue and the finished ones to review.
/// </summary>
public sealed class TestsViewModel : ViewModel
{
    private readonly DrivingBooking _contracts;
    private readonly PracticeTests _tests;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="TestsViewModel"/> class.</summary>
    /// <param name="contracts">The active contracts of the student.</param>
    /// <param name="tests">The tests.</param>
    /// <param name="timeProvider">The clock.</param>
    public TestsViewModel(DrivingBooking contracts, PracticeTests tests, TimeProvider timeProvider)
    {
        _contracts = contracts;
        _tests = tests;
        _timeProvider = timeProvider;
        RefreshCommand = new Command(async () => await RefreshAsync());
    }

    /// <summary>Gets the exams of the student, the latest first.</summary>
    public IReadOnlyList<TestEntry> Exams
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasExams));
        }
    } = [];

    /// <summary>Gets a value indicating whether the student has exams.</summary>
    public bool HasExams => Exams.Count > 0;

    /// <summary>Gets the templates to start a test from.</summary>
    public IReadOnlyList<TestEntry> Templates
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets the attempts of the student, the latest first.</summary>
    public IReadOnlyList<TestEntry> Attempts
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasAttempts));
        }
    } = [];

    /// <summary>Gets a value indicating whether the student has taken tests.</summary>
    public bool HasAttempts => Attempts.Count > 0;

    /// <summary>Gets why there is nothing to show, or <see langword="null"/>.</summary>
    public string? Notice
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasNotice));
        }
    }

    /// <summary>Gets a value indicating whether there is a notice to show.</summary>
    public bool HasNotice => Notice is not null;

    /// <summary>Gets or sets a value indicating whether the page is being refreshed by pulling it down.</summary>
    public bool IsRefreshing
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>Gets the command that reads the templates and the attempts again.</summary>
    public Command RefreshCommand { get; }

    /// <summary>Reads the templates and the attempts.</summary>
    /// <returns>A task that completes when the page shows them.</returns>
    public async Task LoadAsync()
    {
        try
        {
            var contract = (await _contracts.ReadActiveContractsAsync(CancellationToken.None)).Value
                .FirstOrDefault(candidate => candidate is { Id: not null, Category: not null });
            if (contract is not { Id: { } contractId, Category: { } category })
            {
                Exams = [];
                Show([], [], AppTexts.Get("Tests.NoContract"));

                return;
            }

            var templates = await _tests.ReadTemplatesAsync(category, CancellationToken.None);
            var attempts = await _tests.ReadAttemptsAsync(contractId, CancellationToken.None);
            var exams = await _tests.ReadExamsAsync(CancellationToken.None);
            var now = _timeProvider.GetUtcNow();
            Exams = [.. exams.Select(exam => ExamEntry(exam, contractId, attempts, now)).OfType<TestEntry>()];
            Show(
                [.. templates.Select(template => TemplateEntry(contractId, template)).OfType<TestEntry>()],
                [.. attempts.Where(attempt => attempt.ExamId is null).Select(AttemptEntry).OfType<TestEntry>()],
                templates.Count == 0 ? AppTexts.Get("Tests.NoTemplates") : null);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Exams = [];
            Show([], [], AppTexts.Get("Tests.Unavailable"));
        }
    }

    private static Task OpenAttemptAsync(Guid sessionId) =>
        Routes.OpenAsync(Routes.TestSession, new Dictionary<string, object> { [TestSessionViewModel.SessionParameter] = sessionId });

    private static TestEntry? AttemptEntry(TestSessionItem attempt)
    {
        if (attempt is not { Id: { } sessionId, StartedAtUtc: { } startedAt })
            return null;

        var inProgress = attempt.Status == TestSessionStatus.InProgress;
        var details = LessonTexts.DetailsOf(
            startedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture),
            TestTexts.OutcomeOf(attempt.Status, attempt.FailureReason),
            attempt.CorrectAnswerCount is { } correct
                ? AppTexts.Format("Test.Score", correct, attempt.QuestionCount)
                : null);

        return new TestEntry
        {
            Title = attempt.TemplateName ?? string.Empty,
            Details = details,
            ActionText = inProgress ? AppTexts.Get("Tests.Continue") : AppTexts.Get("Tests.Review"),
            Command = new Command(async () => await OpenAttemptAsync(sessionId))
        };
    }

    private TestEntry? ExamEntry(
        StudentExamItem item, Guid contractId, IReadOnlyList<TestSessionItem> attempts, DateTimeOffset now)
    {
        if (item.Exam is not { Id: { } examId, ScheduledAtUtc: { } scheduledAt, Kind: { } kind, Status: { } status } exam)
            return null;

        var title = kind == ExamKind.Theory
            ? exam.TestTemplateName ?? AppTexts.Get("Exams.Theory")
            : AppTexts.Get("Exams.Practice");
        var when = scheduledAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
        if (status == ExamStatus.Cancelled)
            return new TestEntry { Title = title, Details = LessonTexts.DetailsOf(when, AppTexts.Get("Lesson.Cancelled")) };

        if (item.Result is { } result)
        {
            return new TestEntry
            {
                Title = title,
                Details = LessonTexts.DetailsOf(
                    when,
                    result.Passed == true ? AppTexts.Get("Test.Passed") : AppTexts.Get("Test.Failed"),
                    result.Score is { } score ? AppTexts.Format("Exams.Score", score) : null),
                ActionText = result.TestSessionId is null ? null : AppTexts.Get("Tests.Review"),
                Command = result.TestSessionId is { } reviewed ? new Command(async () => await OpenAttemptAsync(reviewed)) : null
            };
        }

        var details = LessonTexts.DetailsOf(when, exam.GroupName);
        if (kind != ExamKind.Theory)
            return new TestEntry { Title = title, Details = details };

        // A theory exam is taken once: an attempt of it is continued, or reviewed until its result is recorded.
        if (attempts.FirstOrDefault(attempt => attempt.ExamId == examId) is { Id: { } sessionId } attempt)
        {
            var inProgress = attempt.Status == TestSessionStatus.InProgress;

            return new TestEntry
            {
                Title = title,
                Details = details,
                ActionText = inProgress ? AppTexts.Get("Tests.Continue") : AppTexts.Get("Tests.Review"),
                Command = new Command(async () => await OpenAttemptAsync(sessionId))
            };
        }

        return scheduledAt <= now
            ? new TestEntry
            {
                Title = title,
                Details = details,
                ActionText = AppTexts.Get("Exams.Start"),
                Command = new Command(async () => await StartExamAsync(examId, contractId))
            }
            : new TestEntry { Title = title, Details = details };
    }

    private TestEntry? TemplateEntry(Guid contractId, TestTemplateItem template)
    {
        if (template is not { Id: { } templateId })
            return null;

        return new TestEntry
        {
            Title = template.Name ?? string.Empty,
            Details = AppTexts.Format("Tests.Rules", template.QuestionCount, template.TimeLimitMinutes, template.MaxMistakes),
            ActionText = AppTexts.Get("Tests.Start"),
            Command = new Command(async () => await StartAsync(contractId, templateId))
        };
    }

    private void Show(IReadOnlyList<TestEntry> templates, IReadOnlyList<TestEntry> attempts, string? notice)
    {
        Templates = templates;
        Attempts = attempts;
        Notice = notice;
    }

    private async Task StartAsync(Guid contractId, Guid templateId)
    {
        Guid sessionId;
        try
        {
            sessionId = await _tests.StartAsync(contractId, templateId, CancellationToken.None);
        }
        catch (ApiException exception) when (exception.ResponseStatusCode == 409)
        {
            // An attempt is in progress, and a contract has one at a time: the list offers to continue it.
            await Shell.Current.DisplayAlertAsync(
                AppTexts.Get("Tests.Title"), AppTexts.Get("Tests.AlreadyInProgress"), AppTexts.Get("Common.Ok"));
            await LoadAsync();

            return;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            await Shell.Current.DisplayAlertAsync(
                AppTexts.Get("Tests.Title"), AppTexts.Get("Tests.Unavailable"), AppTexts.Get("Common.Ok"));

            return;
        }

        await OpenAttemptAsync(sessionId);
    }

    private async Task StartExamAsync(Guid examId, Guid contractId)
    {
        if (!await Shell.Current.DisplayAlertAsync(
            AppTexts.Get("Exams.ConfirmTitle"),
            AppTexts.Get("Exams.ConfirmMessage"),
            AppTexts.Get("Exams.Start"),
            AppTexts.Get("Common.Cancel")))
        {
            return;
        }

        Guid sessionId;
        try
        {
            sessionId = await _tests.StartExamAsync(examId, contractId, CancellationToken.None);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            // The exam may have been taken already, cancelled or not begun on the clock of the API.
            await Shell.Current.DisplayAlertAsync(
                AppTexts.Get("Tests.Title"), AppTexts.Get("Exams.NotStarted"), AppTexts.Get("Common.Ok"));
            await LoadAsync();

            return;
        }

        await OpenAttemptAsync(sessionId);
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
