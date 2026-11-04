namespace AutoElite.Application.Contracts.TerminateContract;

/// <summary>Checks the day and terminates the contract; the domain checks the reason.</summary>
internal sealed class TerminateContractCommandHandler(ContractAccess contractAccess, TimeProvider timeProvider)
    : ICommandHandler<TerminateContractCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(TerminateContractCommand command, CancellationToken cancellationToken)
    {
        var contractResult = await contractAccess.LoadAsync(command.ContractId, cancellationToken);
        if (contractResult.IsFailure)
            return Result.Failure(contractResult.Error);

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var dateResult = ClosingDate.Check(command.TerminatedOn, utcNow);

        return dateResult.IsFailure ? dateResult : contractResult.Value.Terminate(command.Reason, command.TerminatedOn, utcNow);
    }
}
