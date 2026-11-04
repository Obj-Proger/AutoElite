namespace AutoElite.Application.TrainingGrounds.DeactivateTrainingGround;

/// <summary>Stops using a training ground; past lessons keep referring to it.</summary>
/// <param name="GroundId">The identifier of the ground.</param>
[RequireRoles(UserRole.Administrator)]
public sealed record DeactivateTrainingGroundCommand(Guid GroundId) : ICommand;
