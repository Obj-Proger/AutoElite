using AutoElite.Application.TestSessions;

namespace AutoElite.Application.StudentCards.GetStudentCard;

/// <summary>
/// Reads the card and trims it to what the caller may see. A manager sees only the contracts of the branches in
/// their scope, and a student whose contracts are all elsewhere is reported as not found. Counts of a test in
/// progress are hidden (FR-7.7).
/// </summary>
internal sealed class GetStudentCardQueryHandler(
    IStudentCardQueries cardQueries,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IQueryHandler<GetStudentCardQuery, StudentCard>
{
    /// <inheritdoc />
    public async Task<Result<StudentCard>> Handle(GetStudentCardQuery query, CancellationToken cancellationToken)
    {
        var card = await cardQueries.GetAsync(query.StudentId, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        if (card is null)
            return Result.Failure<StudentCard>(ApplicationErrors.NotFound<User>());

        var visible = Trim(card);
        if (visible is null)
            return Result.Failure<StudentCard>(ApplicationErrors.NotFound<User>());

        return visible with
        {
            Contracts =
            [
                .. visible.Contracts.Select(contract => contract with
                {
                    Tests = contract.Tests with
                    {
                        LastAttempt = contract.Tests.LastAttempt is { } attempt ? TestResults.Hide(attempt) : null
                    }
                })
            ]
        };
    }

    private StudentCard? Trim(StudentCard card)
    {
        if (card.StudentId == currentUser.UserId || currentUser.IsInRole(UserRole.Administrator))
            return card;

        if (!currentUser.IsInRole(UserRole.Manager))
            return null;

        var scope = DataScope.For(currentUser);
        StudentCardContract[] contracts = [.. card.Contracts.Where(contract => scope.Includes(contract.Contract.BranchId))];

        return contracts.Length > 0 || scope.Includes(card.BranchId) ? card with { Contracts = contracts } : null;
    }
}
