namespace AutoElite.Application.TrainingGrounds.ReactivateTrainingGround;

/// <summary>Reactivates the ground.</summary>
internal sealed class ReactivateTrainingGroundCommandHandler(IRepository<TrainingGround> groundRepository)
    : ICommandHandler<ReactivateTrainingGroundCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(ReactivateTrainingGroundCommand command, CancellationToken cancellationToken)
    {
        var ground = await groundRepository.GetByIdAsync(command.GroundId, cancellationToken);

        return ground is null ? Result.Failure(ApplicationErrors.NotFound<TrainingGround>()) : ground.Reactivate();
    }
}
