namespace AutoElite.Application.Contracts.CompleteContract;

/// <summary>Checks the day and completes the contract.</summary>
internal sealed class CompleteContractCommandHandler(ContractAccess contractAccess, TimeProvider timeProvider)
    : ICommandHandler<CompleteContractCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(CompleteContractCommand command, CancellationToken cancellationToken)
    {
        var contractResult = await contractAccess.LoadAsync(command.ContractId, cancellationToken);
        if (contractResult.IsFailure)
            return Result.Failure(contractResult.Error);

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var dateResult = ClosingDate.Check(command.CompletedOn, utcNow);

        return dateResult.IsFailure ? dateResult : contractResult.Value.Complete(command.CompletedOn, utcNow);
    }
}
