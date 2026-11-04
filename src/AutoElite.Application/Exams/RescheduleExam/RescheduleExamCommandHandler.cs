namespace AutoElite.Application.Exams.RescheduleExam;

/// <summary>Moves the exam; the domain checks the status, the results and the time.</summary>
internal sealed class RescheduleExamCommandHandler(ExamAccess examAccess, TimeProvider timeProvider)
    : ICommandHandler<RescheduleExamCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(RescheduleExamCommand command, CancellationToken cancellationToken)
    {
        var loadResult = await examAccess.LoadForScheduleAsync(command.ExamId, cancellationToken);

        return loadResult.IsFailure
            ? Result.Failure(loadResult.Error)
            : loadResult.Value.Exam.Reschedule(command.ScheduledAtUtc, timeProvider.GetUtcNow().UtcDateTime);
    }
}
