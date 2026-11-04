namespace AutoElite.Application.TrainingGrounds.DeactivateTrainingGround;

/// <summary>Deactivates the ground.</summary>
internal sealed class DeactivateTrainingGroundCommandHandler(IRepository<TrainingGround> groundRepository)
    : ICommandHandler<DeactivateTrainingGroundCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(DeactivateTrainingGroundCommand command, CancellationToken cancellationToken)
    {
        var ground = await groundRepository.GetByIdAsync(command.GroundId, cancellationToken);

        return ground is null ? Result.Failure(ApplicationErrors.NotFound<TrainingGround>()) : ground.Deactivate();
    }
}
