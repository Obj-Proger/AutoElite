namespace AutoElite.Application.Contracts.ChangeContractPrice;

/// <summary>Changes the price in the contract currency.</summary>
internal sealed class ChangeContractPriceCommandHandler(ContractAccess contractAccess) : ICommandHandler<ChangeContractPriceCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(ChangeContractPriceCommand command, CancellationToken cancellationToken)
    {
        var contractResult = await contractAccess.LoadAsync(command.ContractId, cancellationToken);
        if (contractResult.IsFailure)
            return Result.Failure(contractResult.Error);

        var contract = contractResult.Value;
        var priceResult = Money.Create(command.PriceAmount, contract.Price.Currency);

        return priceResult.IsFailure ? Result.Failure(priceResult.Error) : contract.ChangePrice(priceResult.Value);
    }
}
