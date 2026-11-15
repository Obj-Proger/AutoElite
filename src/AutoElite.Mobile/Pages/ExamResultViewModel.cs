using System.Globalization;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The result of a student at an exam that an instructor records or corrects: whether they passed, a score from 0 to
/// 1000 if the school uses one, and a comment, which a correction needs as its reason. Saving needs the network.
/// </summary>
public sealed class ExamResultViewModel : ViewModel, IQueryAttributable
{
    /// <summary>The navigation parameter that carries the <see cref="ExamResultRequest"/>.</summary>
    public const string ResultParameter = "result";

    // The highest score the API accepts.
    private const int MaxScore = 1000;

    private readonly ExamBook _exams;
    private ExamResultRequest? _request;

    /// <summary>Initializes a new instance of the <see cref="ExamResultViewModel"/> class.</summary>
    /// <param name="exams">The exams.</param>
    public ExamResultViewModel(ExamBook exams)
    {
        _exams = exams;
        PassedCommand = new Command(() => Passed = true);
        FailedCommand = new Command(() => Passed = false);
        SaveCommand = new Command(async () => await SaveAsync(), () => !IsBusy);
    }

    /// <summary>Gets the exam and the student.</summary>
    public string Subject
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the title of the page: recording or correcting a result.</summary>
    public string Title
    {
        get;
        private set => SetProperty(ref field, value);
    } = AppTexts.Get("ExamResult.RecordTitle");

    /// <summary>Gets whether the student passed, or <see langword="null"/> before the instructor chooses.</summary>
    public bool? Passed
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(IsPassed));
                OnPropertyChanged(nameof(IsFailed));
            }
        }
    }

    /// <summary>Gets a value indicating whether the student passed.</summary>
    public bool IsPassed => Passed == true;

    /// <summary>Gets a value indicating whether the student did not pass.</summary>
    public bool IsFailed => Passed == false;

    /// <summary>Gets or sets the score, or empty without one.</summary>
    public string Score
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets or sets the comment, the reason of a correction.</summary>
    public string Comment
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets what the comment is for: a comment, or the reason of the correction.</summary>
    public string CommentHint
    {
        get;
        private set => SetProperty(ref field, value);
    } = AppTexts.Get("ExamResult.Comment");

    /// <summary>Gets why the result could not be saved, or <see langword="null"/>.</summary>
    public string? Message
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasMessage));
        }
    }

    /// <summary>Gets a value indicating whether there is a message.</summary>
    public bool HasMessage => Message is not null;

    /// <summary>Gets a value indicating whether the result is being saved.</summary>
    public bool IsBusy
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                SaveCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets the command that marks the exam passed.</summary>
    public Command PassedCommand { get; }

    /// <summary>Gets the command that marks the exam not passed.</summary>
    public Command FailedCommand { get; }

    /// <summary>Gets the command that saves the result.</summary>
    public Command SaveCommand { get; }

    /// <inheritdoc />
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!query.TryGetValue(ResultParameter, out var value) || value is not ExamResultRequest request)
            return;

        _request = request;
        Subject = request.Title;
        Title = request.IsCorrection ? AppTexts.Get("ExamResult.CorrectTitle") : AppTexts.Get("ExamResult.RecordTitle");
        CommentHint = request.IsCorrection ? AppTexts.Get("ExamResult.Reason") : AppTexts.Get("ExamResult.Comment");
        Passed = request.Passed;
        Score = request.Score?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
    }

    private async Task SaveAsync()
    {
        if (_request is not { } request)
            return;

        if (Passed is not { } passed)
        {
            Message = AppTexts.Get("ExamResult.ChoosePassed");

            return;
        }

        int? score = null;
        if (!string.IsNullOrWhiteSpace(Score))
        {
            if (!int.TryParse(Score.Trim(), NumberStyles.None, CultureInfo.CurrentCulture, out var value) || value > MaxScore)
            {
                Message = AppTexts.Format("ExamResult.BadScore", MaxScore);

                return;
            }

            score = value;
        }

        var comment = string.IsNullOrWhiteSpace(Comment) ? null : Comment.Trim();
        if (request.IsCorrection && comment is null)
        {
            Message = AppTexts.Get("ExamResult.NeedReason");

            return;
        }

        IsBusy = true;
        Message = null;
        try
        {
            if (request.IsCorrection)
                await _exams.CorrectAsync(request.ExamId, request.ContractId, passed, score, comment, CancellationToken.None);
            else
                await _exams.RecordAsync(request.ExamId, request.ContractId, passed, score, comment, CancellationToken.None);

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            Message = ProblemTexts.Of(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
