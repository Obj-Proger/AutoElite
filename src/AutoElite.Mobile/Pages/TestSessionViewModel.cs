using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// A practice test attempt: while it is in progress, the next unanswered question with its illustration and options,
/// the progress and the time left; once it ends, the outcome, the score and the questions answered wrongly with the
/// correct answers and the explanations. The API keeps the timer and decides each answer; when the time runs out on
/// the device, the page closes the attempt on the API.
/// </summary>
public sealed class TestSessionViewModel : ViewModel, IQueryAttributable
{
    /// <summary>The navigation parameter that carries the attempt.</summary>
    public const string SessionParameter = "session";

    private readonly PracticeTests _tests;
    private readonly TimeProvider _timeProvider;
    private Guid _sessionId;
    private DateTimeOffset? _deadline;
    private SessionQuestionItem? _question;
    private bool _isClosing;

    /// <summary>Initializes a new instance of the <see cref="TestSessionViewModel"/> class.</summary>
    /// <param name="tests">The practice tests.</param>
    /// <param name="timeProvider">The clock.</param>
    public TestSessionViewModel(PracticeTests tests, TimeProvider timeProvider)
    {
        _tests = tests;
        _timeProvider = timeProvider;
    }

    /// <summary>Gets the name of the template.</summary>
    public string Title
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets a value indicating whether the attempt is in progress, so a question is shown.</summary>
    public bool IsInProgress
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets a value indicating whether the attempt has ended, so its outcome is shown.</summary>
    public bool IsFinished
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets the position of the question among all of them.</summary>
    public string Progress
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the time left, as minutes and seconds.</summary>
    public string TimeLeft
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the text of the question.</summary>
    public string Question
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the illustration of the question, if it has one.</summary>
    public ImageSource? Image
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasImage));
        }
    }

    /// <summary>Gets a value indicating whether the question has an illustration.</summary>
    public bool HasImage => Image is not null;

    /// <summary>Gets the options of the question, each a button that answers with it.</summary>
    public IReadOnlyList<EntryAction> Options
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets the outcome of the attempt.</summary>
    public string Outcome
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the score of the attempt.</summary>
    public string Score
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the questions answered wrongly, with the correct answers.</summary>
    public IReadOnlyList<MistakeEntry> Mistakes
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets what went wrong with the last request, or <see langword="null"/>.</summary>
    public string? Error
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    /// <summary>Gets a value indicating whether the last request went wrong.</summary>
    public bool HasError => Error is not null;

    /// <inheritdoc />
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue(SessionParameter, out var value) && value is Guid sessionId)
            _sessionId = sessionId;
    }

    /// <summary>Reads the attempt and shows its next question or its outcome.</summary>
    /// <returns>A task that completes when the page shows them.</returns>
    public async Task LoadAsync()
    {
        TestSessionDetails attempt;
        try
        {
            attempt = await _tests.ReadAttemptAsync(_sessionId, CancellationToken.None);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Error = AppTexts.Get("Test.Unavailable");

            return;
        }

        Error = null;
        Title = attempt.Session?.TemplateName ?? string.Empty;
        var questions = (attempt.Questions ?? []).OrderBy(question => question.Position).ToList();
        if (attempt.Session is { Status: TestSessionStatus.InProgress } session)
            await ShowQuestionAsync(session, questions);
        else
            ShowOutcome(attempt.Session, questions);
    }

    /// <summary>Shows the time left, once a second; when it runs out, closes the attempt on the API.</summary>
    /// <returns>A task that completes when the time is shown.</returns>
    public async Task TickAsync()
    {
        if (!IsInProgress || _deadline is not { } deadline)
            return;

        var left = deadline - _timeProvider.GetUtcNow();
        TimeLeft = left > TimeSpan.Zero
            ? string.Create(CultureInfo.InvariantCulture, $"{(int)left.TotalMinutes:00}:{left.Seconds:00}")
            : "00:00";
        if (left > TimeSpan.Zero || _isClosing || _question is not { Id: { } questionId, Options: [{ OptionId: { } optionId }, ..] })
            return;

        // An answer after the deadline closes the attempt without being counted, as the time ran out.
        _isClosing = true;
        try
        {
            await _tests.AnswerAsync(_sessionId, questionId, optionId, CancellationToken.None);
            await LoadAsync();
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Error = AppTexts.Get("Test.Unavailable");
        }
        finally
        {
            _isClosing = false;
        }
    }

    private static string TextOf(SessionQuestionItem question, Guid? optionId) =>
        question.Options?.FirstOrDefault(option => option.OptionId == optionId)?.Text ?? AppTexts.Get("Test.NotAnswered");

    private async Task ShowQuestionAsync(TestSessionItem session, List<SessionQuestionItem> questions)
    {
        var next = questions.FirstOrDefault(question => question.SelectedOptionId is null);
        _deadline = session.DeadlineUtc;
        _question = next;
        IsFinished = false;
        IsInProgress = true;
        if (next is null)
            return;

        Progress = AppTexts.Format("Test.Progress", questions.IndexOf(next) + 1, questions.Count);
        Question = next.Text ?? string.Empty;
        Options =
        [
            .. (next.Options ?? []).Where(option => option.OptionId is not null).Select(option => new EntryAction(
                option.Text ?? string.Empty,
                new Command(async () => await AnswerAsync(next.Id, option.OptionId!.Value))))
        ];
        Image = null;
        if (next.ImageDocumentId is { } imageId)
        {
            try
            {
                var bytes = await _tests.ReadImageAsync(imageId, CancellationToken.None);
                Image = ImageSource.FromStream(() => new MemoryStream(bytes));
            }
            catch (Exception exception) when (exception is ApiException or HttpRequestException)
            {
                // The question is answered without its illustration rather than not at all.
                Image = null;
            }
        }

        await TickAsync();
    }

    private void ShowOutcome(TestSessionItem? session, List<SessionQuestionItem> questions)
    {
        _deadline = null;
        _question = null;
        IsInProgress = false;
        IsFinished = true;
        Outcome = TestTexts.OutcomeOf(session?.Status, session?.FailureReason);
        Score = AppTexts.Format("Test.Score", session?.CorrectAnswerCount ?? 0, questions.Count);
        Mistakes =
        [
            .. questions.Where(question => question.IsCorrect != true).Select(question => new MistakeEntry
            {
                Question = question.Text ?? string.Empty,
                YourAnswer = TextOf(question, question.SelectedOptionId),
                CorrectAnswer = TextOf(question, question.CorrectOptionId),
                Explanation = question.Explanation
            })
        ];
    }

    private async Task AnswerAsync(Guid? questionId, Guid optionId)
    {
        if (questionId is not { } id || _isClosing)
            return;

        try
        {
            await _tests.AnswerAsync(_sessionId, id, optionId, CancellationToken.None);
        }
        catch (ApiException exception) when (exception.ResponseStatusCode == 409)
        {
            // The attempt ended meanwhile, or the question was answered already: the page shows where it stands.
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Error = AppTexts.Get("Test.AnswerNotSent");

            return;
        }

        await LoadAsync();
    }
}
