using System.Globalization;
using AutoElite.Application.Common.Localization;

namespace AutoElite.Application.Analytics;

/// <summary>Lays the reports out as documents with the texts of the reader's language.</summary>
internal static class ReportDocuments
{
    /// <summary>Lays out the lead report.</summary>
    /// <param name="report">The report.</param>
    /// <param name="details">The lines under the title.</param>
    /// <param name="culture">The culture of the reader.</param>
    public static ReportDocument Leads(LeadStatistics report, IReadOnlyList<string> details, CultureInfo culture)
    {
        ReportTable summary = new(
            Text(ReportTexts.TableSummary, culture),
            [Column(ReportTexts.ColumnLeads, ReportValueKind.Count, culture),
             Column(ReportTexts.ColumnConverted, ReportValueKind.Count, culture),
             Column(ReportTexts.ColumnConversion, ReportValueKind.Share, culture)],
            [[report.Total, report.Converted, report.ConversionRate]]);
        ReportTable bySource = new(
            Text(ReportTexts.TableBySource, culture),
            [Column(ReportTexts.ColumnSource, ReportValueKind.Text, culture),
             Column(ReportTexts.ColumnLeads, ReportValueKind.Count, culture),
             Column(ReportTexts.ColumnConverted, ReportValueKind.Count, culture),
             Column(ReportTexts.ColumnConversion, ReportValueKind.Share, culture)],
            [.. report.BySource.Select(row => Row(ReportTexts.Name(row.Source, culture), row.Total, row.Converted, row.ConversionRate))]);
        ReportTable byStatus = new(
            Text(ReportTexts.TableByStatus, culture),
            [Column(ReportTexts.ColumnStatus, ReportValueKind.Text, culture),
             Column(ReportTexts.ColumnLeads, ReportValueKind.Count, culture)],
            [.. report.ByStatus.Select(row => Row(ReportTexts.Name(row.Status, culture), row.Count))]);

        return new ReportDocument(Text(ReportTexts.LeadStatisticsTitle, culture), details, [summary, bySource, byStatus], culture);
    }

    /// <summary>Lays out the revenue report.</summary>
    /// <param name="months">The revenue of each month and currency.</param>
    /// <param name="details">The lines under the title.</param>
    /// <param name="culture">The culture of the reader.</param>
    public static ReportDocument Revenue(IReadOnlyList<MonthlyRevenue> months, IReadOnlyList<string> details, CultureInfo culture)
    {
        ReportTable table = new(
            null,
            [Column(ReportTexts.ColumnMonth, ReportValueKind.Month, culture),
             Column(ReportTexts.ColumnCurrency, ReportValueKind.Text, culture),
             Column(ReportTexts.ColumnReceived, ReportValueKind.Money, culture),
             Column(ReportTexts.ColumnRefunded, ReportValueKind.Money, culture),
             Column(ReportTexts.ColumnNet, ReportValueKind.Money, culture)],
            [.. months.Select(row => Row(new DateOnly(row.Year, row.Month, 1), row.Currency, row.Received, row.Refunded, row.Net))]);

        return new ReportDocument(Text(ReportTexts.RevenueTitle, culture), details, [table], culture);
    }

    /// <summary>Lays out the contract report.</summary>
    /// <param name="summary">The summary.</param>
    /// <param name="details">The lines under the title.</param>
    /// <param name="culture">The culture of the reader.</param>
    public static ReportDocument Contracts(ContractSummary summary, IReadOnlyList<string> details, CultureInfo culture)
    {
        ReportTable table = new(
            null,
            [Column(ReportTexts.ColumnContracts, ReportValueKind.Count, culture),
             Column(ReportTexts.ColumnActive, ReportValueKind.Count, culture),
             Column(ReportTexts.ColumnCompleted, ReportValueKind.Count, culture),
             Column(ReportTexts.ColumnTerminated, ReportValueKind.Count, culture),
             Column(ReportTexts.ColumnCompletionRate, ReportValueKind.Share, culture),
             Column(ReportTexts.ColumnAverageQuality, ReportValueKind.Number, culture)],
            [[summary.Total, summary.Active, summary.Completed, summary.Terminated, summary.CompletionRate, summary.AverageQualityScore]]);

        return new ReportDocument(Text(ReportTexts.ContractSummaryTitle, culture), details, [table], culture);
    }

    /// <summary>Lays out the instructor report.</summary>
    /// <param name="instructors">The work of each instructor.</param>
    /// <param name="details">The lines under the title.</param>
    /// <param name="culture">The culture of the reader.</param>
    public static ReportDocument Instructors(
        IReadOnlyList<InstructorWorkloadItem> instructors, IReadOnlyList<string> details, CultureInfo culture)
    {
        ReportTable table = new(
            null,
            [Column(ReportTexts.ColumnInstructor, ReportValueKind.Text, culture),
             Column(ReportTexts.ColumnDrivingLessons, ReportValueKind.Count, culture),
             Column(ReportTexts.ColumnDrivingHours, ReportValueKind.Number, culture),
             Column(ReportTexts.ColumnCancellations, ReportValueKind.Count, culture),
             Column(ReportTexts.ColumnNoShows, ReportValueKind.Count, culture),
             Column(ReportTexts.ColumnAverageRating, ReportValueKind.Number, culture),
             Column(ReportTexts.ColumnTheoryLessons, ReportValueKind.Count, culture),
             Column(ReportTexts.ColumnTheoryHours, ReportValueKind.Number, culture)],
            [
                .. instructors.Select(row => Row(
                    row.InstructorName, row.DrivingLessons, row.DrivingHours, row.DrivingCancellations, row.DrivingNoShows,
                    row.AverageRating, row.TheoryLessons, row.TheoryHours))
            ]);

        return new ReportDocument(Text(ReportTexts.InstructorWorkloadTitle, culture), details, [table], culture);
    }

    /// <summary>Lays out the staff report.</summary>
    /// <param name="staff">The tasks of each staff member.</param>
    /// <param name="details">The lines under the title.</param>
    /// <param name="culture">The culture of the reader.</param>
    public static ReportDocument Staff(IReadOnlyList<StaffActivityItem> staff, IReadOnlyList<string> details, CultureInfo culture)
    {
        ReportTable table = new(
            null,
            [Column(ReportTexts.ColumnStaffMember, ReportValueKind.Text, culture),
             Column(ReportTexts.ColumnTasksSet, ReportValueKind.Count, culture),
             Column(ReportTexts.ColumnTasksCompleted, ReportValueKind.Count, culture),
             Column(ReportTexts.ColumnTasksOverdue, ReportValueKind.Count, culture)],
            [.. staff.Select(row => Row(row.Name, row.Assigned, row.Completed, row.Overdue))]);

        return new ReportDocument(Text(ReportTexts.StaffActivityTitle, culture), details, [table], culture);
    }

    private static string Text(string key, CultureInfo culture) => ReportTexts.Format(key, culture);

    private static ReportColumn Column(string key, ReportValueKind kind, CultureInfo culture) => new(Text(key, culture), kind);

    private static object?[] Row(params object?[] values) => values;
}
