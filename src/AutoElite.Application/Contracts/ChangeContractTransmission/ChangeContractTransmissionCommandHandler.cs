namespace AutoElite.Application.Contracts.ChangeContractTransmission;

/// <summary>Changes the gearbox; the domain checks it.</summary>
internal sealed class ChangeContractTransmissionCommandHandler(ContractAccess contractAccess)
    : ICommandHandler<ChangeContractTransmissionCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(ChangeContractTransmissionCommand command, CancellationToken cancellationToken)
    {
        var contractResult = await contractAccess.LoadAsync(command.ContractId, cancellationToken);

        return contractResult.IsFailure
            ? Result.Failure(contractResult.Error)
            : contractResult.Value.ChangeTransmission(command.Transmission);
    }
}
