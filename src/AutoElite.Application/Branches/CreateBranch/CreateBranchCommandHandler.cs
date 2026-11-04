namespace AutoElite.Application.Branches.CreateBranch;

/// <summary>Checks that the name is free and creates the branch.</summary>
internal sealed class CreateBranchCommandHandler(IBranchRepository branchRepository) : ICommandHandler<CreateBranchCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateBranchCommand command, CancellationToken cancellationToken)
    {
        var detailsResult = BranchDetails.Create(command.City, command.Street, command.Phone);
        if (detailsResult.IsFailure)
            return Result.Failure<Guid>(detailsResult.Error);

        var (address, phone) = detailsResult.Value;
        var branchResult = Branch.Create(command.Name, address, phone, command.TimeZoneId);
        if (branchResult.IsFailure)
            return Result.Failure<Guid>(branchResult.Error);

        var branch = branchResult.Value;
        if (await branchRepository.NameExistsAsync(branch.Name, cancellationToken: cancellationToken))
            return Result.Failure<Guid>(ApplicationErrors.Branch.NameTaken);

        branchRepository.Add(branch);

        return branch.Id;
    }
}
