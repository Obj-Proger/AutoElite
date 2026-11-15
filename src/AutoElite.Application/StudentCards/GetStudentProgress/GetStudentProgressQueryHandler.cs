using AutoElite.Application.TestSessions;

namespace AutoElite.Application.StudentCards.GetStudentProgress;

/// <summary>
/// Reads the card of a student the caller teaches and keeps what an instructor needs; a student the caller does not
/// teach is reported as not found, like one who does not exist. Counts of a test in progress are hidden (FR-7.7).
/// </summary>
internal sealed class GetStudentProgressQueryHandler(
    IStudentCardQueries cardQueries,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IQueryHandler<GetStudentProgressQuery, StudentProgress>
{
    /// <inheritdoc />
    public async Task<Result<StudentProgress>> Handle(GetStudentProgressQuery query, CancellationToken cancellationToken)
    {
        if (!await cardQueries.IsTaughtByAsync(query.StudentId, currentUser.GetRequiredUserId(), cancellationToken))
            return Result.Failure<StudentProgress>(ApplicationErrors.NotFound<User>());

        var card = await cardQueries.GetAsync(query.StudentId, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        if (card is null)
            return Result.Failure<StudentProgress>(ApplicationErrors.NotFound<User>());

        return new StudentProgress(
            card.StudentId,
            card.LastName,
            card.FirstName,
            card.MiddleName,
            card.Email,
            card.Phone,
            card.PhotoDocumentId,
            [.. card.Contracts.Select(Progress)]);
    }

    private static StudentProgressContract Progress(StudentCardContract contract)
    {
        var details = contract.Contract;
        var tests = contract.Tests with
        {
            LastAttempt = contract.Tests.LastAttempt is { } attempt ? TestResults.Hide(attempt) : null
        };

        return new StudentProgressContract(
            details.Id,
            details.CourseName,
            details.Category,
            details.Transmission,
            details.Status,
            details.StartDate,
            details.EndDate,
            details.CompletedPracticeHours,
            details.CoursePracticeHours,
            contract.Group,
            contract.Driving,
            tests,
            contract.Exams);
    }
}
