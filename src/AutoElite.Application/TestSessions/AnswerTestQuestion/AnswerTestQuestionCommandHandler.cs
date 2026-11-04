namespace AutoElite.Application.TestSessions.AnswerTestQuestion;

/// <summary>
/// Records the answer, draws the questions added for a mistake and returns the status of the attempt, so that the
/// client knows whether to go on or show the result.
/// </summary>
internal sealed class AnswerTestQuestionCommandHandler(
    ITestSessionRepository sessionRepository,
    IRepository<TestTemplate> templateRepository,
    SessionQuestions sessionQuestions,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<AnswerTestQuestionCommand, TestSessionStatus>
{
    /// <inheritdoc />
    public async Task<Result<TestSessionStatus>> Handle(AnswerTestQuestionCommand command, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(command.SessionId, cancellationToken);
        if (session is null || session.StudentId != currentUser.GetRequiredUserId())
            return Result.Failure<TestSessionStatus>(ApplicationErrors.NotFound<TestSession>());

        // A late answer closes the attempt and is saved as such, rather than failing and leaving the attempt open.
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        if (session.Status == TestSessionStatus.InProgress && utcNow >= session.DeadlineUtc)
        {
            session.Expire(utcNow);
            return session.Status;
        }

        var answerResult = session.Answer(command.SessionQuestionId, command.OptionId, utcNow);
        if (answerResult.IsFailure)
            return Result.Failure<TestSessionStatus>(answerResult.Error);

        if (session.PendingAddOnBlocks.Count > 0)
        {
            var template = await templateRepository.GetByIdAsync(session.TemplateId, cancellationToken);
            var addResult = await sessionQuestions.AddPendingAsync(session, template?.Source, cancellationToken);
            if (addResult.IsFailure)
                return Result.Failure<TestSessionStatus>(addResult.Error);
        }

        return session.Status;
    }
}
