namespace AutoElite.Application.Contracts.RescheduleContract;

/// <summary>Changes the period; the domain checks it.</summary>
internal sealed class RescheduleContractCommandHandler(ContractAccess contractAccess) : ICommandHandler<RescheduleContractCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(RescheduleContractCommand command, CancellationToken cancellationToken)
    {
        var contractResult = await contractAccess.LoadAsync(command.ContractId, cancellationToken);

        return contractResult.IsFailure
            ? Result.Failure(contractResult.Error)
            : contractResult.Value.Reschedule(command.StartDate, command.EndDate);
    }
}
