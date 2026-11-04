namespace AutoElite.Application.TrainingGrounds.CreateTrainingGround;

/// <summary>Checks the branch and adds the ground.</summary>
internal sealed class CreateTrainingGroundCommandHandler(
    IRepository<TrainingGround> groundRepository,
    IRepository<Branch> branchRepository) : ICommandHandler<CreateTrainingGroundCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateTrainingGroundCommand command, CancellationToken cancellationToken)
    {
        var addressResult = Address.Create(command.City, command.Street);
        if (addressResult.IsFailure)
            return Result.Failure<Guid>(addressResult.Error);

        var groundResult = TrainingGround.Create(command.Name, addressResult.Value, command.Description, command.BranchId);
        if (groundResult.IsFailure)
            return Result.Failure<Guid>(groundResult.Error);

        // Only administrators manage the grounds, and they see every branch.
        var branchResult = await BranchChecks.CheckAsync(branchRepository, command.BranchId, DataScope.SchoolWide, cancellationToken);
        if (branchResult.IsFailure)
            return Result.Failure<Guid>(branchResult.Error);

        groundRepository.Add(groundResult.Value);

        return groundResult.Value.Id;
    }
}
