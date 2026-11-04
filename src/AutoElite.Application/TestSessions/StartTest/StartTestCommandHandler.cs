using AutoElite.Application.Contracts;

namespace AutoElite.Application.TestSessions.StartTest;

/// <summary>Checks that the contract is the caller's, loads the template and starts the attempt.</summary>
internal sealed class StartTestCommandHandler(
    IContractRepository contractRepository,
    IRepository<TestTemplate> templateRepository,
    TestAttempts testAttempts,
    ICurrentUser currentUser) : ICommandHandler<StartTestCommand, Guid>
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

        return await testAttempts.StartAsync(contract, template, null, cancellationToken);
    }
}
