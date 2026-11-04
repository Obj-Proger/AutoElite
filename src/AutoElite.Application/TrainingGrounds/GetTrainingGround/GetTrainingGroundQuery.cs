namespace AutoElite.Application.TrainingGrounds.GetTrainingGround;

/// <summary>Reads one training ground. A driving instructor sees the grounds of their branch and the shared ones.</summary>
/// <param name="GroundId">The identifier of the ground.</param>
[RequireRoles(UserRole.Administrator, UserRole.DrivingInstructor)]
public sealed record GetTrainingGroundQuery(Guid GroundId) : IQuery<TrainingGroundItem>;
