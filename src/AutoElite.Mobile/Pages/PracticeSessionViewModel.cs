using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// A practice of a student, question by question: the question with its illustration and its options; once the student
/// taps an option, the API checks it, the correct option is marked green, a wrong choice red, and the explanation is
/// shown before the next question. There is no timer and no outcome, only the count of right and wrong answers; at the
/// end the student goes through the questions again in another order or practises their mistakes. The questions are
/// read in parts as the student goes, all in the order of one number chosen when the practice starts.
/// </summary>
public sealed class PracticeSessionViewModel : ViewModel, IQueryAttributable
{
    /// <summary>The name of the parameter with the <see cref="PracticeRequest"/>.</summary>
    public const string PracticeParameter = "practice";

    // The questions read at a time: enough for a few minutes of practice, few enough to read quickly.
    private const int PartSize = 20;

    // The most the API gives at once, for the mistakes, which are read again from the start.
    private const int MaxPartSize = 50;

    private readonly QuestionPractice _practice;
    private readonly PracticeTests _tests;
    private readonly List<PracticeQuestion> _questions = [];
    private readonly HashSet<Guid> _shown = [];
    private PracticeRequest? _request;
    private int _seed;
    private int _total;
    private int _position;
    private PracticeQuestion? _current;
    private bool _isStarted;

    // The right and wrong answers of this practice.
    private int _right;
    private int _wrong;

    /// <summary>Initializes a new instance of the <see cref="PracticeSessionViewModel"/> class.</summary>
    /// <param name="practice">The practice of the questions.</param>
    /// <param name="tests">The tests, for the illustrations of the questions.</param>
    public PracticeSessionViewModel(QuestionPractice practice, PracticeTests tests)
    {
        _practice = practice;
        _tests = tests;
        NextCommand = new Command(async () => await NextAsync(), () => IsAnswered && !IsBusy);
        AgainCommand = new Command(async () => await RestartAsync());
        MistakesCommand = new Command(async () => await PractiseMistakesAsync());
    }

    /// <summary>Gets the name of the practice.</summary>
    public string Title
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the number of the question of all of them.</summary>
    public string Progress
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the share of the questions gone through, from 0 to 1.</summary>
    public double QuestionProgress
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets the right and wrong answers so far.</summary>
    public string Score
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

    /// <summary>Gets the options of the question.</summary>
    public IReadOnlyList<PracticeOptionEntry> Options
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets a value indicating whether the question is answered and the next one can be shown.</summary>
    public bool IsAnswered
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                NextCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets a value indicating whether the last answer was right.</summary>
    public bool IsRight
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets whether the last answer was right or wrong, in words.</summary>
    public string Verdict
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the explanation of the correct answer, if the question has one.</summary>
    public string? Explanation
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasExplanation));
        }
    }

    /// <summary>Gets a value indicating whether the question has an explanation.</summary>
    public bool HasExplanation => !string.IsNullOrWhiteSpace(Explanation);

    /// <summary>Gets a value indicating whether a question is shown.</summary>
    public bool IsInProgress
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets a value indicating whether every question has been gone through.</summary>
    public bool IsFinished
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets the right answers of all the answers, at the end.</summary>
    public string Outcome
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets a value indicating whether the student answered some questions wrong, to practise them after.</summary>
    public bool HasMistakes => _wrong > 0;

    /// <summary>Gets what the page says when the practice has no questions, or <see langword="null"/>.</summary>
    public string? EmptyText
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets why the questions could not be read or an answer could not be checked.</summary>
    public string? Error
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets a value indicating whether questions are read or an answer is checked.</summary>
    public bool IsBusy
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                NextCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets the command that shows the next question.</summary>
    public Command NextCommand { get; }

    /// <summary>Gets the command that goes through the questions again, in another order.</summary>
    public Command AgainCommand { get; }

    /// <summary>Gets the command that practises the mistakes.</summary>
    public Command MistakesCommand { get; }

    /// <inheritdoc />
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!query.TryGetValue(PracticeParameter, out var value) || value is not PracticeRequest request)
            return;

        _request = request;
        Title = request.Title;
        _isStarted = false;
    }

    /// <summary>Starts the practice the first time the page appears; coming back to it keeps the question shown.</summary>
    /// <returns>A task that completes when the first question is shown.</returns>
    public async Task LoadAsync()
    {
        if (_isStarted || _request is null)
            return;

        _isStarted = true;
        await RestartAsync();
    }

    private async Task RestartAsync()
    {
        _seed = Guid.NewGuid().GetHashCode();
        _questions.Clear();
        _shown.Clear();
        _total = 0;
        _position = 0;
        _right = 0;
        _wrong = 0;
        OnPropertyChanged(nameof(HasMistakes));
        IsFinished = false;
        EmptyText = null;
        if (await ReadPartAsync())
            await ShowAsync();
    }

    // Reads the next part of the practice; false when it could not be read. A question answered right leaves the
    // mistakes on the API, which shifts the later ones, so the mistakes are read from the start again and those
    // already shown are skipped; their number stays the one the practice started with.
    private async Task<bool> ReadPartAsync()
    {
        if (_request is not { } request)
            return false;

        var isMistakes = request.Mode == PracticeMode.Mistakes;
        IsBusy = true;
        try
        {
            var page = await _practice.ReadQuestionsAsync(
                request.ContractId,
                request.Mode,
                request.TopicId,
                _seed,
                isMistakes ? 0 : _questions.Count,
                isMistakes ? MaxPartSize : PartSize,
                CancellationToken.None);
            if (!isMistakes || _questions.Count == 0)
                _total = page.Total ?? 0;

            _questions.AddRange((page.Questions ?? []).Where(question => question.Id is { } id && _shown.Add(id)));
            Error = null;

            return true;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            Error = ProblemTexts.Of(exception);

            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ShowAsync()
    {
        if (_position >= _questions.Count)
        {
            Finish();

            return;
        }

        _current = _questions[_position];
        IsInProgress = true;
        IsAnswered = false;
        Verdict = string.Empty;
        Explanation = null;
        Question = _current.Text ?? string.Empty;
        Progress = AppTexts.Format("Test.Progress", _position + 1, _total);
        QuestionProgress = _total == 0 ? 0 : (double)_position / _total;
        Score = AppTexts.Format("Practice.Score", _right, _wrong);
        Options = [.. OptionsOf(_current, null)];
        await ShowImageAsync(_current.ImageDocumentId);
    }

    private async Task ShowImageAsync(Guid? imageId)
    {
        Image = null;
        if (imageId is not { } documentId)
            return;

        try
        {
            var bytes = await _tests.ReadImageAsync(documentId, CancellationToken.None);
            if (_current?.ImageDocumentId == documentId)
                Image = ImageSource.FromStream(() => new MemoryStream(bytes));
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            // The question is shown without its illustration.
        }
    }

    // The options of a question, tappable before it is answered and marked after.
    private IEnumerable<PracticeOptionEntry> OptionsOf(PracticeQuestion question, (Guid Chosen, Guid Correct)? answer) =>
        (question.Options ?? [])
            .Where(option => option.Id is not null)
            .Select(option => answer is { } checkedAnswer
                ? new PracticeOptionEntry(
                    option.Text ?? string.Empty,
                    null,
                    option.Id == checkedAnswer.Correct,
                    option.Id == checkedAnswer.Chosen && checkedAnswer.Chosen != checkedAnswer.Correct)
                : new PracticeOptionEntry(
                    option.Text ?? string.Empty,
                    new Command(async () => await AnswerAsync(question, option.Id!.Value)),
                    false,
                    false));

    private async Task AnswerAsync(PracticeQuestion question, Guid optionId)
    {
        if (IsAnswered || IsBusy || _request is not { } request || question.Id is not { } questionId)
            return;

        IsBusy = true;
        try
        {
            var result = await _practice.AnswerAsync(request.ContractId, questionId, optionId, CancellationToken.None);
            var isRight = result.IsCorrect == true;
            if (isRight)
                _right++;
            else
                _wrong++;

            OnPropertyChanged(nameof(HasMistakes));
            IsRight = isRight;
            Verdict = isRight ? AppTexts.Get("Practice.Right") : AppTexts.Get("Practice.Wrong");
            Explanation = result.Explanation;
            Score = AppTexts.Format("Practice.Score", _right, _wrong);
            Options = [.. OptionsOf(question, (optionId, result.CorrectOptionId ?? Guid.Empty))];
            Error = null;
            IsAnswered = true;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            Error = ProblemTexts.Of(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task NextAsync()
    {
        _position++;
        if (_position >= _questions.Count && _questions.Count < _total && !await ReadPartAsync())
        {
            _position--;

            return;
        }

        await ShowAsync();
    }

    private void Finish()
    {
        IsInProgress = false;
        Image = null;
        _current = null;
        if (_right + _wrong == 0)
        {
            EmptyText = _request?.Mode == PracticeMode.Mistakes
                ? AppTexts.Get("Practice.NoMistakes")
                : AppTexts.Get("Practice.NoQuestions");

            return;
        }

        IsFinished = true;
        Outcome = AppTexts.Format("Practice.Outcome", _right, _right + _wrong);
        QuestionProgress = 1;
    }

    private async Task PractiseMistakesAsync()
    {
        if (_request is not { } request)
            return;

        await Routes.ReplaceAsync(
            Routes.PracticeSession,
            new Dictionary<string, object>
            {
                [PracticeParameter] = new PracticeRequest(
                    request.ContractId, PracticeMode.Mistakes, null, AppTexts.Get("Practice.Mistakes"))
            });
    }
}
