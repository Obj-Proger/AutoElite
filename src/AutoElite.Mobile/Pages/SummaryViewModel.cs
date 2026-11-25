using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Controls;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The summary of the school for an administrator or a manager, for this month, the last one or the last three: the
/// key figures first, the money, the leads with their conversion, the contracts drawn up and the driving lessons, then
/// the leads by source, the work of the instructors and the overdue tasks of the staff. The figures are read from the
/// API or, without a network, from the device; each of them is downloaded for the period as a PDF document or an Excel
/// workbook, which needs the network. The figures of a branch or a course are on the website.
/// </summary>
public sealed class SummaryViewModel : ViewModel
{
    private readonly SchoolSummary _summary;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="SummaryViewModel"/> class.</summary>
    /// <param name="summary">The figures of the school.</param>
    /// <param name="timeProvider">The clock, for the periods.</param>
    public SummaryViewModel(SchoolSummary summary, TimeProvider timeProvider)
    {
        _summary = summary;
        _timeProvider = timeProvider;
        ShowPeriodCommand = new Command<string>(async period => await ShowAsync(Enum.Parse<SummaryPeriod>(period)));
        RefreshCommand = new Command(async () => await RefreshAsync());
        ReportCommand = new Command(async () => await ChooseReportAsync(), () => !IsDownloading);
    }

    /// <summary>Gets the period shown.</summary>
    public SummaryPeriod Period
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(IsMonth));
                OnPropertyChanged(nameof(IsLastMonth));
                OnPropertyChanged(nameof(IsQuarter));
            }
        }
    }

    /// <summary>Gets a value indicating whether this month is shown.</summary>
    public bool IsMonth => Period == SummaryPeriod.Month;

    /// <summary>Gets a value indicating whether the last month is shown.</summary>
    public bool IsLastMonth => Period == SummaryPeriod.LastMonth;

    /// <summary>Gets a value indicating whether the last three months are shown.</summary>
    public bool IsQuarter => Period == SummaryPeriod.Quarter;

    /// <summary>Gets the days of the period.</summary>
    public string Days
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the money received less the refunds, per currency.</summary>
    public string Revenue
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the money received and refunded.</summary>
    public string RevenueDetails
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets how many leads arrived.</summary>
    public string Leads
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the share of the leads that became contracts.</summary>
    public string Conversion
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the share of the leads that became contracts, from 0 to 1.</summary>
    public double ConversionProgress
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets how many contracts were drawn up.</summary>
    public string Contracts
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the share of the closed contracts that were completed.</summary>
    public string Completion
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the share of the closed contracts that were completed, from 0 to 1, or -1 when none is closed.</summary>
    public double CompletionProgress
    {
        get;
        private set => SetProperty(ref field, value);
    } = -1;

    /// <summary>Gets how many driving lessons there were.</summary>
    public string Lessons
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the hours of the driving lessons and the no-shows.</summary>
    public string LessonDetails
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the leads by source, the largest first.</summary>
    public IReadOnlyList<SummaryRow> Sources
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasNoSources));
        }
    } = [];

    /// <summary>Gets a value indicating whether the period has no sources of leads.</summary>
    public bool HasNoSources => Sources.Count == 0;

    /// <summary>Gets the instructors, the busiest first.</summary>
    public IReadOnlyList<SummaryRow> Instructors
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasNoInstructors));
        }
    } = [];

    /// <summary>Gets a value indicating whether the period has no instructors with lessons.</summary>
    public bool HasNoInstructors => Instructors.Count == 0;

    /// <summary>Gets the staff members with tasks, those with overdue tasks first.</summary>
    public IReadOnlyList<SummaryRow> Staff
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasNoStaff));
        }
    } = [];

    /// <summary>Gets a value indicating whether the period has no staff members with tasks.</summary>
    public bool HasNoStaff => Staff.Count == 0;

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

    /// <summary>Gets the command that shows a period, given by the name of a <see cref="SummaryPeriod"/>.</summary>
    public Command<string> ShowPeriodCommand { get; }

    /// <summary>Gets the command that downloads a report of the period.</summary>
    public Command ReportCommand { get; }

    /// <summary>Gets a value indicating whether a report is being downloaded.</summary>
    public bool IsDownloading
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                ReportCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets the command that reads the figures again.</summary>
    public Command RefreshCommand { get; }

    /// <summary>Reads the figures of the period shown.</summary>
    /// <returns>A task that completes when the page shows them.</returns>
    public Task LoadAsync() => ShowAsync(Period);

    // The share of a part in a whole, or nothing for an empty whole; worked out here from the counts the API gives.
    private static double? ShareOf(int part, int whole) => whole == 0 ? null : (double)part / whole;

    private static string PercentOf(double? share) =>
        share is { } value ? value.ToString("P0", CultureInfo.CurrentCulture) : "—";

    private static string NumberOf(double value) => value.ToString("0.#", CultureInfo.CurrentCulture);

    private static string MoneyOf(IEnumerable<(string Currency, double Amount)> amounts)
    {
        var sums = amounts.GroupBy(amount => amount.Currency, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => ContractsViewModel.MoneyOf(group.Sum(amount => amount.Amount), group.Key))
            .ToList();

        return sums.Count == 0 ? ContractsViewModel.MoneyOf(0, null).Trim() : string.Join("; ", sums);
    }

    private static SummaryRow SourceRow(LeadSourceStatistics source) => new()
    {
        Icon = "inbox",
        Title = LeadTexts.SourceOf(source.Source) ?? AppTexts.Get("Summary.OtherSource"),
        Subtitle = AppTexts.Format("Summary.Converted", source.Converted ?? 0),
        Trailing = (source.Total ?? 0).ToString(CultureInfo.CurrentCulture),
        Detail = PercentOf(ShareOf(source.Converted ?? 0, source.Total ?? 0))
    };

    private static SummaryRow InstructorRow(InstructorWorkloadItem instructor) => new()
    {
        Icon = "user",
        Tone = (instructor.DrivingNoShows ?? 0) > 0 ? StatusTone.Warning : StatusTone.Neutral,
        Title = instructor.InstructorName ?? string.Empty,
        Subtitle = AppTexts.Format(
            "Summary.Workload",
            instructor.DrivingLessons ?? 0,
            NumberOf((double)(instructor.DrivingHours ?? 0)),
            instructor.TheoryLessons ?? 0,
            instructor.DrivingNoShows ?? 0),
        Trailing = instructor.AverageRating is { } rating ? "★ " + NumberOf((double)rating) : "—",
        Detail = AppTexts.Get("Summary.Rating")
    };

    private static SummaryRow StaffRow(StaffActivityItem member) => new()
    {
        Icon = "clipboard-check",
        Tone = (member.Overdue ?? 0) > 0 ? StatusTone.Danger : StatusTone.Neutral,
        Title = member.Name ?? string.Empty,
        Subtitle = AppTexts.Format("Summary.Tasks", member.Assigned ?? 0, member.Completed ?? 0),
        Trailing = (member.Overdue ?? 0).ToString(CultureInfo.CurrentCulture),
        Detail = AppTexts.Get("Summary.Overdue")
    };

    // This month and the last three end today; the last month is whole.
    private (DateOnly From, DateOnly To) DaysOf(SummaryPeriod period)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        var month = new DateOnly(today.Year, today.Month, 1);

        return period switch
        {
            SummaryPeriod.LastMonth => (month.AddMonths(-1), month.AddDays(-1)),
            SummaryPeriod.Quarter => (month.AddMonths(-2), today),
            _ => (month, today)
        };
    }

    private async Task ShowAsync(SummaryPeriod period)
    {
        Period = period;
        var (from, to) = DaysOf(period);
        var culture = CultureInfo.CurrentCulture;
        Days = string.Create(culture, $"{from.ToString("d MMMM", culture)} — {to.ToString("d MMMM yyyy", culture)}");
        try
        {
            var leads = await _summary.ReadLeadsAsync(from, to, CancellationToken.None);
            var revenue = await _summary.ReadRevenueAsync(from, to, CancellationToken.None);
            var contracts = await _summary.ReadContractsAsync(from, to, CancellationToken.None);
            var workload = await _summary.ReadWorkloadAsync(from, to, CancellationToken.None);
            var staff = await _summary.ReadStaffAsync(from, to, CancellationToken.None);
            if (period != Period)
                return;

            ShowMoney(revenue.Value);
            ShowLeads(leads.Value);
            ShowContracts(contracts.Value);
            ShowWork(workload.Value);
            Staff =
            [
                .. staff.Value.Where(member => (member.Assigned ?? 0) > 0 || (member.Overdue ?? 0) > 0)
                    .OrderByDescending(member => member.Overdue ?? 0)
                    .ThenBy(member => member.Name, StringComparer.CurrentCulture)
                    .Select(StaffRow)
            ];
            DateTimeOffset?[] savedAt =
            [
                leads.IsFromCache ? leads.SavedAt : null,
                revenue.IsFromCache ? revenue.SavedAt : null,
                contracts.IsFromCache ? contracts.SavedAt : null,
                workload.IsFromCache ? workload.SavedAt : null,
                staff.IsFromCache ? staff.SavedAt : null
            ];
            Notice = savedAt.Where(saved => saved is not null).Min() is { } oldest
                ? AppTexts.Format("Summary.Offline", oldest.ToLocalTime().ToString("g", culture))
                : null;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Notice = ProblemTexts.Of(exception);
        }
    }

    private void ShowMoney(IReadOnlyList<MonthlyRevenue> months)
    {
        Revenue = MoneyOf(
            months.Select(month => (month.Currency ?? string.Empty, (double)(month.Received ?? 0) - (double)(month.Refunded ?? 0))));
        RevenueDetails = AppTexts.Format(
            "Summary.RevenueDetails",
            MoneyOf(months.Select(month => (month.Currency ?? string.Empty, (double)(month.Received ?? 0)))),
            MoneyOf(months.Select(month => (month.Currency ?? string.Empty, (double)(month.Refunded ?? 0)))));
    }

    private void ShowLeads(LeadStatistics leads)
    {
        var conversion = ShareOf(leads.Converted ?? 0, leads.Total ?? 0);
        Leads = (leads.Total ?? 0).ToString(CultureInfo.CurrentCulture);
        Conversion = AppTexts.Format("Summary.Conversion", PercentOf(conversion));
        ConversionProgress = conversion ?? 0;
        Sources =
        [
            .. (leads.BySource ?? []).Where(source => (source.Total ?? 0) > 0)
                .OrderByDescending(source => source.Total)
                .Select(SourceRow)
        ];
    }

    private void ShowContracts(ContractSummary contracts)
    {
        var completed = contracts.Completed ?? 0;
        var completion = ShareOf(completed, completed + (contracts.Terminated ?? 0));
        Contracts = (completed + (contracts.Terminated ?? 0) + (contracts.Active ?? 0)).ToString(CultureInfo.CurrentCulture);
        Completion = AppTexts.Format("Summary.Completion", PercentOf(completion), contracts.Active ?? 0);
        CompletionProgress = completion ?? -1;
    }

    private void ShowWork(IReadOnlyList<InstructorWorkloadItem> instructors)
    {
        Lessons = instructors.Sum(instructor => instructor.DrivingLessons ?? 0).ToString(CultureInfo.CurrentCulture);
        LessonDetails = AppTexts.Format(
            "Summary.LessonDetails",
            NumberOf(instructors.Sum(instructor => (double)(instructor.DrivingHours ?? 0))),
            instructors.Sum(instructor => instructor.DrivingNoShows ?? 0));
        Instructors =
        [
            .. instructors.Where(instructor => (instructor.DrivingLessons ?? 0) + (instructor.TheoryLessons ?? 0) > 0)
                .OrderByDescending(instructor => (double)(instructor.DrivingHours ?? 0) + (double)(instructor.TheoryHours ?? 0))
                .Select(InstructorRow)
        ];
    }

    // The figures of the report, then its format; the report covers the period shown, for the whole school.
    private async Task ChooseReportAsync()
    {
        (string Kind, string Name)[] kinds =
        [
            ("Revenue", AppTexts.Get("Summary.Revenue")),
            ("LeadStatistics", AppTexts.Get("Summary.Leads")),
            ("ContractSummary", AppTexts.Get("Summary.Contracts")),
            ("InstructorWorkload", AppTexts.Get("Summary.Instructors")),
            ("StaffActivity", AppTexts.Get("Summary.Staff"))
        ];
        var kindName = await Shell.Current.DisplayActionSheetAsync(
            AppTexts.Get("Summary.Report"), AppTexts.Get("Common.Cancel"), null, [.. kinds.Select(kind => kind.Name)]);
        var chosen = kinds.FirstOrDefault(kind => kind.Name == kindName);
        if (chosen.Name is null)
            return;

        var pdf = AppTexts.Get("Summary.Pdf");
        var excel = AppTexts.Get("Summary.Excel");
        var formatName = await Shell.Current.DisplayActionSheetAsync(chosen.Name, AppTexts.Get("Common.Cancel"), null, pdf, excel);
        if (formatName != pdf && formatName != excel)
            return;

        var (from, to) = DaysOf(Period);
        IsDownloading = true;
        try
        {
            await _summary.OpenReportAsync(
                chosen.Kind, formatName == excel ? ReportFormat.Excel : ReportFormat.Pdf, from, to, CancellationToken.None);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            Notice = ProblemTexts.Of(exception);
        }
        catch (IOException)
        {
            Notice = AppTexts.Get("Summary.ReportFailed");
        }
        finally
        {
            IsDownloading = false;
        }
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
