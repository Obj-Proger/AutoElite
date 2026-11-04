namespace AutoElite.Application.Exams.CancelExam;

/// <summary>Cancels the exam; the domain checks the status, the results and the reason.</summary>
internal sealed class CancelExamCommandHandler(ExamAccess examAccess, TimeProvider timeProvider) : ICommandHandler<CancelExamCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(CancelExamCommand command, CancellationToken cancellationToken)
    {
        var loadResult = await examAccess.LoadForScheduleAsync(command.ExamId, cancellationToken);

        return loadResult.IsFailure
            ? Result.Failure(loadResult.Error)
            : loadResult.Value.Exam.Cancel(command.Reason, timeProvider.GetUtcNow().UtcDateTime);
    }
}
