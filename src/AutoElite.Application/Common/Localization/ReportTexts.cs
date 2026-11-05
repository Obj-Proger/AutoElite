using System.Globalization;
using System.Resources;

namespace AutoElite.Application.Common.Localization;

/// <summary>
/// The texts of exported reports, from the <c>ReportTexts</c> resources: English by default and a translation for every
/// other supported language. The constants are the keys; values of enums are <c>{Type}.{Member}</c> texts.
/// </summary>
internal static class ReportTexts
{
    /// <summary>The title of the lead report.</summary>
    public const string LeadStatisticsTitle = "LeadStatistics.Title";

    /// <summary>The title of the revenue report.</summary>
    public const string RevenueTitle = "Revenue.Title";

    /// <summary>The title of the contract report.</summary>
    public const string ContractSummaryTitle = "ContractSummary.Title";

    /// <summary>The title of the instructor report.</summary>
    public const string InstructorWorkloadTitle = "InstructorWorkload.Title";

    /// <summary>The title of the staff report.</summary>
    public const string StaffActivityTitle = "StaffActivity.Title";

    /// <summary>The period line: <c>{0}</c> is the first day, <c>{1}</c> the last day.</summary>
    public const string Period = "Period";

    /// <summary>The branch line: <c>{0}</c> is the branch name.</summary>
    public const string Branch = "Branch";

    /// <summary>The branch line of a report on the whole school.</summary>
    public const string WholeSchool = "WholeSchool";

    /// <summary>The line with the time the report was made: <c>{0}</c> is the local time.</summary>
    public const string CreatedAt = "CreatedAt";

    /// <summary>The title of a table of totals.</summary>
    public const string TableSummary = "Table.Summary";

    /// <summary>The title of the leads by source.</summary>
    public const string TableBySource = "Table.BySource";

    /// <summary>The title of the leads by status.</summary>
    public const string TableByStatus = "Table.ByStatus";

    /// <summary>A column header.</summary>
    public const string ColumnLeads = "Column.Leads";

    /// <summary>A column header.</summary>
    public const string ColumnConverted = "Column.Converted";

    /// <summary>A column header.</summary>
    public const string ColumnConversion = "Column.Conversion";

    /// <summary>A column header.</summary>
    public const string ColumnSource = "Column.Source";

    /// <summary>A column header.</summary>
    public const string ColumnStatus = "Column.Status";

    /// <summary>A column header.</summary>
    public const string ColumnMonth = "Column.Month";

    /// <summary>A column header.</summary>
    public const string ColumnCurrency = "Column.Currency";

    /// <summary>A column header.</summary>
    public const string ColumnReceived = "Column.Received";

    /// <summary>A column header.</summary>
    public const string ColumnRefunded = "Column.Refunded";

    /// <summary>A column header.</summary>
    public const string ColumnNet = "Column.Net";

    /// <summary>A column header.</summary>
    public const string ColumnContracts = "Column.Contracts";

    /// <summary>A column header.</summary>
    public const string ColumnActive = "Column.Active";

    /// <summary>A column header.</summary>
    public const string ColumnCompleted = "Column.Completed";

    /// <summary>A column header.</summary>
    public const string ColumnTerminated = "Column.Terminated";

    /// <summary>A column header.</summary>
    public const string ColumnCompletionRate = "Column.CompletionRate";

    /// <summary>A column header.</summary>
    public const string ColumnAverageQuality = "Column.AverageQuality";

    /// <summary>A column header.</summary>
    public const string ColumnInstructor = "Column.Instructor";

    /// <summary>A column header.</summary>
    public const string ColumnDrivingLessons = "Column.DrivingLessons";

    /// <summary>A column header.</summary>
    public const string ColumnDrivingHours = "Column.DrivingHours";

    /// <summary>A column header.</summary>
    public const string ColumnCancellations = "Column.Cancellations";

    /// <summary>A column header.</summary>
    public const string ColumnNoShows = "Column.NoShows";

    /// <summary>A column header.</summary>
    public const string ColumnAverageRating = "Column.AverageRating";

    /// <summary>A column header.</summary>
    public const string ColumnTheoryLessons = "Column.TheoryLessons";

    /// <summary>A column header.</summary>
    public const string ColumnTheoryHours = "Column.TheoryHours";

    /// <summary>A column header.</summary>
    public const string ColumnStaffMember = "Column.StaffMember";

    /// <summary>A column header.</summary>
    public const string ColumnTasksSet = "Column.TasksSet";

    /// <summary>A column header.</summary>
    public const string ColumnTasksCompleted = "Column.TasksCompleted";

    /// <summary>A column header.</summary>
    public const string ColumnTasksOverdue = "Column.TasksOverdue";

    /// <summary>Gets the report text resources.</summary>
    internal static ResourceManager Resources { get; } =
        new("AutoElite.Application.Common.Localization.ReportTexts", typeof(ReportTexts).Assembly);

    /// <summary>Gets a text in the culture and fills in its placeholders.</summary>
    /// <param name="key">The key of the text, one of the constants of this class.</param>
    /// <param name="culture">The culture of the reader.</param>
    /// <param name="arguments">The values of the placeholders, in order.</param>
    public static string Format(string key, CultureInfo culture, params object?[] arguments) =>
        string.Format(culture, Get(key, culture), arguments);

    /// <summary>Gets the name of a lead source in the culture.</summary>
    /// <param name="source">The source.</param>
    /// <param name="culture">The culture of the reader.</param>
    public static string Name(LeadSource source, CultureInfo culture) => Get($"{nameof(LeadSource)}.{source}", culture);

    /// <summary>Gets the name of a lead status in the culture.</summary>
    /// <param name="status">The status.</param>
    /// <param name="culture">The culture of the reader.</param>
    public static string Name(LeadStatus status, CultureInfo culture) => Get($"{nameof(LeadStatus)}.{status}", culture);

    private static string Get(string key, CultureInfo culture) =>
        Resources.GetString(key, culture) ?? throw new InvalidOperationException($"The report text '{key}' is missing.");
}
