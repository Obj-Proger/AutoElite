using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The questions the school wrote for the practice tests, the latest added first, found by their text and read a page
/// at a time; a theory instructor opens one to correct it or writes a new one. They need the network.
/// </summary>
public sealed class QuestionsViewModel : ViewModel
{
    private readonly QuestionBank _bank;
    private readonly List<QuestionEntry> _items = [];
    private int _page;
    private int _total;
    private int _version;

    /// <summary>Initializes a new instance of the <see cref="QuestionsViewModel"/> class.</summary>
    /// <param name="bank">The questions of the school.</param>
    public QuestionsViewModel(QuestionBank bank)
    {
        _bank = bank;
        SearchCommand = new Command(async () => await LoadAsync());
        MoreCommand = new Command(async () => await ReadPageAsync(_page + 1, _version), () => HasMore && !IsBusy);
        NewCommand = new Command(async () => await Routes.OpenAsync(Routes.Question));
    }

    /// <summary>Gets or sets the text to find in the questions.</summary>
    public string Search
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the questions read so far.</summary>
    public IReadOnlyList<QuestionEntry> Items
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets how many questions match, that none do, or why they could not be read.</summary>
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

    /// <summary>Gets a value indicating whether more questions match than are read.</summary>
    public bool HasMore => _items.Count < _total;

    /// <summary>Gets a value indicating whether a page is being read.</summary>
    public bool IsBusy
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                MoreCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets the command that finds the questions with the text.</summary>
    public Command SearchCommand { get; }

    /// <summary>Gets the command that reads the next page.</summary>
    public Command MoreCommand { get; }

    /// <summary>Gets the command that opens a new question.</summary>
    public Command NewCommand { get; }

    /// <summary>Reads the first page of the questions that match, each time the page appears.</summary>
    /// <returns>A task that completes when the page shows them.</returns>
    public async Task LoadAsync()
    {
        var version = ++_version;
        _items.Clear();
        _total = 0;
        await ReadPageAsync(1, version);
    }

    private static string StatusOf(QuestionListItem question) =>
        string.Join(
            " · ",
            new[]
            {
                question.HasImage == true ? AppTexts.Get("Questions.HasImage") : null,
                question.IsActive == false ? AppTexts.Get("Questions.Inactive") : null
            }.OfType<string>());

    // Reads a page; a page of an earlier search that arrives late is dropped.
    private async Task ReadPageAsync(int page, int version)
    {
        IsBusy = true;
        try
        {
            var found = await _bank.ReadPageAsync(Search, page, CancellationToken.None);
            if (version != _version)
                return;

            _page = page;
            _total = found.TotalCount ?? 0;
            _items.AddRange((found.Items ?? []).Where(question => question.Id is not null).Select(Entry));
            Items = [.. _items];
            Notice = _total == 0 ? AppTexts.Get("Questions.None") : AppTexts.Format("Questions.Count", _total);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            if (version == _version)
                Notice = ProblemTexts.Of(exception);
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(HasMore));
            MoreCommand.ChangeCanExecute();
        }
    }

    private static QuestionEntry Entry(QuestionListItem question)
    {
        var questionId = question.Id!.Value;

        return new QuestionEntry
        {
            Text = question.Text ?? string.Empty,
            Details = LessonTexts.DetailsOf(
                question.TopicTitle,
                question.CourseName,
                string.Join(", ", (question.Categories ?? []).OfType<LicenseCategory>().Select(CategoryNames.Of))),
            Status = StatusOf(question),
            IsActive = question.IsActive != false,
            OpenCommand = new Command(async () => await Routes.OpenAsync(
                Routes.Question, new Dictionary<string, object> { [QuestionViewModel.QuestionParameter] = questionId }))
        };
    }
}
