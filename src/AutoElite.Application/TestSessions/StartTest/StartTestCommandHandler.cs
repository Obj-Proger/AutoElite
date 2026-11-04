using AutoElite.Application.Contracts;

namespace AutoElite.Application.TestSessions.StartTest;

/// <summary>
/// Closes an attempt of the contract whose time has run out, draws the questions and starts the new attempt.
/// </summary>
internal sealed class StartTestCommandHandler(
    ITestSessionRepository sessionRepository,
    IContractRepository contractRepository,
    IRepository<TestTemplate> templateRepository,
    SessionQuestions sessionQuestions,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<StartTestCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(StartTestCommand command, CancellationToken cancellationToken)
    {
        var contract = await contractRepository.GetByIdAsync(command.ContractId, cancellationToken);
        if (contract is null || contract.StudentId != currentUser.GetRequiredUserId())
            return Result.Failure<Guid>(ApplicationErrors.NotFound<Contract>());

        var template = await templateRepository.GetByIdAsync(command.TemplateId, cancellationToken);
        if (template is null)
            return Result.Failure<Guid>(ApplicationErrors.NotFound<TestTemplate>());

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

        var sessionResult = TestSession.Start(contract, template, questionsResult.Value, null, utcNow);
        if (sessionResult.IsFailure)
            return Result.Failure<Guid>(sessionResult.Error);

        sessionRepository.Add(sessionResult.Value);

        return sessionResult.Value.Id;
    }
}
