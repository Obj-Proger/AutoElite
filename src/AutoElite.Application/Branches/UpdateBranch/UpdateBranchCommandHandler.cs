namespace AutoElite.Application.Branches.UpdateBranch;

/// <summary>Checks that the name is free among the other branches and applies the details.</summary>
internal sealed class UpdateBranchCommandHandler(IBranchRepository branchRepository) : ICommandHandler<UpdateBranchCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(UpdateBranchCommand command, CancellationToken cancellationToken)
    {
        var branch = await branchRepository.GetByIdAsync(command.BranchId, cancellationToken);
        if (branch is null)
            return Result.Failure(ApplicationErrors.NotFound<Branch>());

        var detailsResult = BranchDetails.Create(command.City, command.Street, command.Phone);
        if (detailsResult.IsFailure)
            return Result.Failure(detailsResult.Error);

        var (address, phone) = detailsResult.Value;
        var updateResult = branch.UpdateDetails(command.Name, address, phone, command.TimeZoneId);
        if (updateResult.IsFailure)
            return updateResult;

        return await branchRepository.NameExistsAsync(branch.Name, branch.Id, cancellationToken)
            ? Result.Failure(ApplicationErrors.Branch.NameTaken)
            : Result.Success();
    }
}
