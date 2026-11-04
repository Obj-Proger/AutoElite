namespace AutoElite.Application.TrainingGrounds.UpdateTrainingGround;

/// <summary>Applies the details and checks the branch when the ground moves to another one.</summary>
internal sealed class UpdateTrainingGroundCommandHandler(
    IRepository<TrainingGround> groundRepository,
    IRepository<Branch> branchRepository) : ICommandHandler<UpdateTrainingGroundCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(UpdateTrainingGroundCommand command, CancellationToken cancellationToken)
    {
        var ground = await groundRepository.GetByIdAsync(command.GroundId, cancellationToken);
        if (ground is null)
            return Result.Failure(ApplicationErrors.NotFound<TrainingGround>());

        var addressResult = Address.Create(command.City, command.Street);
        if (addressResult.IsFailure)
            return Result.Failure(addressResult.Error);

        var previousBranchId = ground.BranchId;
        var updateResult = ground.UpdateDetails(command.Name, addressResult.Value, command.Description, command.BranchId);
        if (updateResult.IsFailure || ground.BranchId == previousBranchId)
            return updateResult;

        // A ground can stay in a branch that has since closed; only a move is checked. Only administrators manage the
        // grounds, and they see every branch.
        return await BranchChecks.CheckAsync(branchRepository, ground.BranchId, DataScope.SchoolWide, cancellationToken);
    }
}
