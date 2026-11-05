namespace AutoElite.Application.Analytics;

/// <summary>Computes shares for the reports.</summary>
internal static class Shares
{
    /// <summary>Gets the share of a part in a whole, from 0 to 1 with four digits, or <see langword="null"/> for an empty whole.</summary>
    /// <param name="part">The part.</param>
    /// <param name="whole">The whole.</param>
    public static decimal? Of(int part, int whole) => whole == 0 ? null : Math.Round((decimal)part / whole, 4);
}

/// <summary>The leads that arrived in the period (FR-2.7, FR-14.2).</summary>
/// <param name="Total">The leads that arrived.</param>
/// <param name="Converted">Of them, the leads converted to a contract by now.</param>
/// <param name="BySource">The leads by source, every source listed.</param>
/// <param name="ByStatus">The leads by their current status, every status listed.</param>
public sealed record LeadStatistics(
    int Total, int Converted, IReadOnlyList<LeadSourceStatistics> BySource, IReadOnlyList<LeadStatusCount> ByStatus)
{
    /// <summary>Gets the share of converted leads, or <see langword="null"/> if no lead arrived.</summary>
    public decimal? ConversionRate => Shares.Of(Converted, Total);
}

/// <summary>The leads of one source.</summary>
/// <param name="Source">The source.</param>
/// <param name="Total">The leads that arrived from it.</param>
/// <param name="Converted">Of them, the leads converted by now.</param>
public sealed record LeadSourceStatistics(LeadSource Source, int Total, int Converted)
{
    /// <summary>Gets the share of converted leads, or <see langword="null"/> if none arrived.</summary>
    public decimal? ConversionRate => Shares.Of(Converted, Total);
}

/// <summary>The leads in one status.</summary>
/// <param name="Status">The status.</param>
/// <param name="Count">The leads in it.</param>
public sealed record LeadStatusCount(LeadStatus Status, int Count);

/// <summary>The money received in one month in one currency (FR-14.1).</summary>
/// <param name="Year">The year.</param>
/// <param name="Month">The month, from 1 to 12, in the time zone of the report.</param>
/// <param name="Currency">The ISO 4217 code of the currency; currencies are never added together.</param>
/// <param name="Received">The payments confirmed in the month.</param>
/// <param name="Refunded">The payments refunded in the month.</param>
public sealed record MonthlyRevenue(int Year, int Month, string Currency, decimal Received, decimal Refunded)
{
    /// <summary>Gets the received money less the refunds.</summary>
    public decimal Net => Received - Refunded;
}

/// <summary>The contracts drawn up in the period, by their current status (FR-14.3).</summary>
/// <param name="Active">The contracts still active.</param>
/// <param name="Completed">The contracts completed.</param>
/// <param name="Terminated">The contracts terminated.</param>
/// <param name="AverageQualityScore">The average quality score of the contracts that have one.</param>
public sealed record ContractSummary(int Active, int Completed, int Terminated, decimal? AverageQualityScore)
{
    /// <summary>Gets the contracts drawn up.</summary>
    public int Total => Active + Completed + Terminated;

    /// <summary>Gets the share of completed contracts among the closed ones, or <see langword="null"/> if none is closed.</summary>
    public decimal? CompletionRate => Shares.Of(Completed, Completed + Terminated);
}

/// <summary>The work of one instructor on lessons that started in the period (FR-14.4).</summary>
/// <param name="InstructorId">The instructor.</param>
/// <param name="InstructorName">The full name of the instructor.</param>
/// <param name="DrivingLessons">The driving lessons completed.</param>
/// <param name="DrivingHours">The driving hours given.</param>
/// <param name="DrivingCancellations">The driving bookings cancelled.</param>
/// <param name="DrivingNoShows">The driving lessons the student did not come to.</param>
/// <param name="AverageRating">The students' average rating of the driving lessons, if any were rated.</param>
/// <param name="TheoryLessons">The theory lessons held.</param>
/// <param name="TheoryHours">The theory hours given.</param>
public sealed record InstructorWorkloadItem(
    Guid InstructorId,
    string InstructorName,
    int DrivingLessons,
    decimal DrivingHours,
    int DrivingCancellations,
    int DrivingNoShows,
    decimal? AverageRating,
    int TheoryLessons,
    decimal TheoryHours);

/// <summary>The tasks of one staff member in the period (FR-14.5).</summary>
/// <param name="UserId">The staff member.</param>
/// <param name="Name">The full name.</param>
/// <param name="Assigned">The tasks set for them in the period.</param>
/// <param name="Completed">The tasks they completed in the period.</param>
/// <param name="Overdue">The tasks due in the period that were not completed by the deadline.</param>
public sealed record StaffActivityItem(Guid UserId, string Name, int Assigned, int Completed, int Overdue);
