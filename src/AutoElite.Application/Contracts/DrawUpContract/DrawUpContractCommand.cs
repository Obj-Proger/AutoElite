namespace AutoElite.Application.Contracts.DrawUpContract;

/// <summary>
/// Draws up a training contract for an existing student (FR-4.1), for example a former student who starts another
/// category. The contract goes to the given branch, otherwise to the student's or the manager's branch.
/// </summary>
/// <param name="StudentId">The student.</param>
/// <param name="CourseId">The course.</param>
/// <param name="Transmission">The gearbox the student trains on.</param>
/// <param name="StartDate">The first day of training.</param>
/// <param name="EndDate">The planned last day of training, later than the start.</param>
/// <param name="PriceAmount">The agreed price in the course currency; empty for the course price.</param>
/// <param name="Number">The number of the paper contract, unique; empty to give out the next number.</param>
/// <param name="BranchId">The branch; empty for the student's or the manager's branch.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record DrawUpContractCommand(
    Guid StudentId,
    Guid CourseId,
    Transmission Transmission,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal? PriceAmount = null,
    string? Number = null,
    Guid? BranchId = null) : ICommand<Guid>;
