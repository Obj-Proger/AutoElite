using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Controls;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The hub of the practice of a student on the questions of the category of their first active contract: how many
/// questions they have answered of all of them, then the marathon of every question, their mistakes, the exam tickets,
/// which are the practice tests, and each topic with its answers and mistakes. It is read from the API or, without a
/// network, from the device; a practice itself needs the network.
/// </summary>
public sealed class PracticeViewModel : ViewModel
{
    private readonly DrivingBooking _contracts;
    private readonly QuestionPractice _practice;
    private Guid? _contractId;

    // How many mistakes the student had when the page was read, so that an empty practice of them is not started.
    private int _mistakes;

    /// <summary>Initializes a new instance of the <see cref="PracticeViewModel"/> class.</summary>
    /// <param name="contracts">The active contracts of the student.</param>
    /// <param name="practice">The practice of the questions.</param>
    public PracticeViewModel(DrivingBooking contracts, QuestionPractice practice)
    {
        _contracts = contracts;
        _practice = practice;
        MarathonCommand = new Command(async () => await StartAsync(PracticeMode.Marathon, null, AppTexts.Get("Practice.Marathon")));
        MistakesCommand = new Command(async () => await StartMistakesAsync());
        TicketsCommand = new Command(async () => await Routes.OpenAsync(Routes.Tests));
        RefreshCommand = new Command(async () => await RefreshAsync());
    }

    /// <summary>Gets how many questions the student has answered of all of them.</summary>
    public string Answered
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the share of the questions answered, from 0 to 1.</summary>
    public double AnsweredProgress
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets how many questions the marathon has.</summary>
    public string MarathonDetails
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets how many mistakes the student has to practise.</summary>
    public string MistakeDetails
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets how many mistakes the student has to practise, at the end of their row.</summary>
    public string MistakeCount
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the tone of the mistakes: danger when there are some.</summary>
    public StatusTone MistakeTone
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets the topics, each opening its practice.</summary>
    public IReadOnlyList<SummaryRow> Topics
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets a value indicating whether the practice can be taken: a contract with questions is there.</summary>
    public bool HasQuestions
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets what the page says when there is nothing to practise, or <see langword="null"/>.</summary>
    public string? EmptyText
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets that the figures are from the device, or why they could not be read.</summary>
    public string? Notice
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets or sets a value indicating whether the page is pulled down to read the figures again.</summary>
    public bool IsRefreshing
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>Gets the command that starts the marathon of every question.</summary>
    public Command MarathonCommand { get; }

    /// <summary>Gets the command that starts the practice of the mistakes.</summary>
    public Command MistakesCommand { get; }

    /// <summary>Gets the command that opens the exam tickets, the practice tests.</summary>
    public Command TicketsCommand { get; }

    /// <summary>Gets the command that reads the figures again.</summary>
    public Command RefreshCommand { get; }

    /// <summary>Reads the contract and how far the student has gone through its questions.</summary>
    /// <returns>A task that completes when the page shows them.</returns>
    public async Task LoadAsync()
    {
        try
        {
            var contract = (await _contracts.ReadActiveContractsAsync(CancellationToken.None)).Value
                .FirstOrDefault(candidate => candidate is { Id: not null, Category: not null });
            if (contract?.Id is not { } contractId)
            {
                Show(null, AppTexts.Get("Tests.NoContract"));

                return;
            }

            _contractId = contractId;
            var overview = await _practice.ReadOverviewAsync(contractId, CancellationToken.None);
            Notice = overview.IsFromCache
                ? AppTexts.Format("Practice.Offline", overview.SavedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture))
                : null;
            Show(overview.Value, (overview.Value.QuestionCount ?? 0) == 0 ? AppTexts.Get("Practice.NoQuestions") : null);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Notice = ProblemTexts.Of(exception);
        }
    }

    private void Show(PracticeOverview? overview, string? empty)
    {
        var total = overview?.QuestionCount ?? 0;
        var answered = overview?.AnsweredCount ?? 0;
        _mistakes = overview?.MistakeCount ?? 0;
        HasQuestions = total > 0;
        EmptyText = empty;
        Answered = AppTexts.Format("Practice.Answered", answered, total);
        AnsweredProgress = total == 0 ? 0 : (double)answered / total;
        MarathonDetails = AppTexts.Format("Practice.MarathonDetails", total);
        MistakeDetails = _mistakes == 0 ? AppTexts.Get("Practice.NoMistakes") : AppTexts.Get("Practice.MistakesDetails");
        MistakeCount = _mistakes.ToString(CultureInfo.CurrentCulture);
        MistakeTone = _mistakes > 0 ? StatusTone.Danger : StatusTone.Success;
        Topics = [.. (overview?.Topics ?? []).Where(topic => topic.TopicId is not null).Select(TopicRow)];
    }

    private SummaryRow TopicRow(PracticeTopic topic)
    {
        var count = topic.QuestionCount ?? 0;
        var answered = topic.AnsweredCount ?? 0;
        var mistakes = topic.MistakeCount ?? 0;
        var title = topic.Title ?? string.Empty;
        var topicId = topic.TopicId!.Value;

        return new SummaryRow
        {
            Icon = "book-open",
            Tone = mistakes > 0 ? StatusTone.Warning : answered >= count && count > 0 ? StatusTone.Success : StatusTone.Accent,
            Title = title,
            Subtitle = AppTexts.Format("Practice.TopicDetails", answered, count, mistakes),
            Trailing = count == 0 ? "—" : ((double)answered / count).ToString("P0", CultureInfo.CurrentCulture),
            Command = new Command(async () => await StartAsync(PracticeMode.Topic, topicId, title))
        };
    }

    private async Task StartMistakesAsync()
    {
        if (_mistakes == 0)
        {
            await Shell.Current.DisplayAlertAsync(
                AppTexts.Get("Practice.Mistakes"), AppTexts.Get("Practice.NoMistakes"), AppTexts.Get("Common.Ok"));

            return;
        }

        await StartAsync(PracticeMode.Mistakes, null, AppTexts.Get("Practice.Mistakes"));
    }

    private async Task StartAsync(PracticeMode mode, Guid? topicId, string title)
    {
        if (_contractId is not { } contractId)
            return;

        await Routes.OpenAsync(
            Routes.PracticeSession,
            new Dictionary<string, object>
            {
                [PracticeSessionViewModel.PracticeParameter] = new PracticeRequest(contractId, mode, topicId, title)
            });
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
