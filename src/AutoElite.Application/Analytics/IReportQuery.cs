namespace AutoElite.Application.Analytics;

/// <summary>A report query over a period of calendar days, optionally for one branch (M-14).</summary>
public interface IReportQuery
{
    /// <summary>Gets the first day of the period, in the local time of the branch or the school.</summary>
    DateOnly From { get; }

    /// <summary>Gets the last day of the period, included.</summary>
    DateOnly To { get; }

    /// <summary>Gets the branch to report on; <see langword="null"/> for the whole school, or a manager's own branch.</summary>
    Guid? BranchId { get; }
}
