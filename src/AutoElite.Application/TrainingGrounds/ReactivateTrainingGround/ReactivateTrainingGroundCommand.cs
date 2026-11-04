namespace AutoElite.Application.TrainingGrounds.ReactivateTrainingGround;

/// <summary>Starts using a training ground again.</summary>
/// <param name="GroundId">The identifier of the ground.</param>
[RequireRoles(UserRole.Administrator)]
public sealed record ReactivateTrainingGroundCommand(Guid GroundId) : ICommand;
