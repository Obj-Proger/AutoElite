namespace AutoElite.Application.Exams.CorrectExamResult;

/// <summary>Corrects the result; the domain checks that it exists and the values.</summary>
internal sealed class CorrectExamResultCommandHandler(ExamAccess examAccess, ICurrentUser currentUser, TimeProvider timeProvider)
    : ICommandHandler<CorrectExamResultCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(CorrectExamResultCommand command, CancellationToken cancellationToken)
    {
        var loadResult = await examAccess.LoadForResultsAsync(command.ExamId, cancellationToken);
        if (loadResult.IsFailure)
            return loadResult;

        return loadResult.Value.Exam.CorrectResult(
            command.ContractId, command.Passed, command.Score, command.Comment, currentUser.GetRequiredUserId(),
            timeProvider.GetUtcNow().UtcDateTime);
    }
}
