namespace AutoElite.Application.TestSessions;

/// <summary>Starts test attempts, practice ones and those of theory exams.</summary>
internal sealed class TestAttempts(
    ITestSessionRepository sessionRepository,
    SessionQuestions sessionQuestions,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Closes an attempt of the contract whose time has run out, draws the questions and starts a new attempt. A
    /// contract has one attempt in progress at a time.
    /// </summary>
    /// <param name="contract">The contract of the student.</param>
    /// <param name="template">The template of the attempt.</param>
    /// <param name="examId">The exam the attempt belongs to, or <see langword="null"/> for a practice test.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The identifier of the attempt, or a failure.</returns>
    public async Task<Result<Guid>> StartAsync(
        Contract contract, TestTemplate template, Guid? examId, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var unfinished = await sessionRepository.GetInProgressOfContractAsync(contract.Id, cancellationToken);
        if (unfinished is not null)
        {
            if (utcNow < unfinished.DeadlineUtc)
                return Result.Failure<Guid>(ApplicationErrors.TestSession.AlreadyInProgress);

            unfinished.Expire(utcNow);
        }

        var questionsResult = await sessionQuestions.DrawBlocksAsync(template, cancellationToken);
        if (questionsResult.IsFailure)
            return Result.Failure<Guid>(questionsResult.Error);

        var sessionResult = TestSession.Start(contract, template, questionsResult.Value, examId, utcNow);
        if (sessionResult.IsFailure)
            return Result.Failure<Guid>(sessionResult.Error);

        sessionRepository.Add(sessionResult.Value);

        return sessionResult.Value.Id;
    }
}
