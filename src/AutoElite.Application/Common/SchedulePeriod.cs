namespace AutoElite.Application.Common;

/// <summary>
/// The time a schedule query of theory or practice reads, limited so that one call cannot read the whole history.
/// </summary>
internal static class SchedulePeriod
{
    /// <summary>The longest period a schedule query can read, enough for a quarter or a calendar month view.</summary>
    public static readonly TimeSpan MaxLength = TimeSpan.FromDays(92);

    /// <summary>Creates the period from two UTC instants.</summary>
    /// <param name="fromUtc">The start of the period in UTC.</param>
    /// <param name="toUtc">The end of the period in UTC, later than the start.</param>
    /// <returns>The period, or a failure if an instant is not in UTC, the end is not later, or the period is too long.</returns>
    public static Result<TimeInterval> Create(DateTime fromUtc, DateTime toUtc)
    {
        var periodResult = TimeInterval.Create(fromUtc, toUtc);
        if (periodResult.IsFailure)
            return periodResult;

        return periodResult.Value.Duration > MaxLength
            ? Result.Failure<TimeInterval>(ApplicationErrors.Schedule.PeriodTooLong)
            : periodResult;
    }
}
