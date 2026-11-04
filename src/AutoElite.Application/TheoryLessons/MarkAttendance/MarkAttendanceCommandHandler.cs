using AutoElite.Application.Contracts;

namespace AutoElite.Application.TheoryLessons.MarkAttendance;

/// <summary>Applies every mark with its contract; the first failure stops the command and nothing is saved.</summary>
internal sealed class MarkAttendanceCommandHandler(
    TheoryLessonAccess lessonAccess,
    IContractRepository contractRepository,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<MarkAttendanceCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(MarkAttendanceCommand command, CancellationToken cancellationToken)
    {
        var loadResult = await lessonAccess.LoadAsync(command.LessonId, cancellationToken);
        if (loadResult.IsFailure)
            return Result.Failure(loadResult.Error);

        var (lesson, group) = loadResult.Value;
        var markedBy = currentUser.GetRequiredUserId();
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var mark in command.Marks ?? [])
        {
            var contract = await contractRepository.GetByIdAsync(mark.ContractId, cancellationToken);
            if (contract is null)
                return Result.Failure(DomainErrors.TheoryLesson.NotGroupMember);

            var markResult = lesson.MarkAttendance(group, contract, mark.IsPresent, mark.Comment, markedBy, utcNow);
            if (markResult.IsFailure)
                return markResult;
        }

        return Result.Success();
    }
}
