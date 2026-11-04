namespace AutoElite.Application.Contracts;

/// <summary>Checks the day a contract is closed on.</summary>
internal static class ClosingDate
{
    /// <summary>
    /// Rejects a day in the future. The current day is taken in UTC with one day of slack, so that a branch east of
    /// UTC can close a contract on its own today.
    /// </summary>
    /// <param name="closedOn">The day of completion or termination.</param>
    /// <param name="utcNow">The current UTC time.</param>
    public static Result Check(DateOnly closedOn, DateTime utcNow) =>
        closedOn > DateOnly.FromDateTime(utcNow).AddDays(1)
            ? Result.Failure(ApplicationErrors.Contract.ClosedInFuture)
            : Result.Success();
}
