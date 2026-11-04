namespace AutoElite.Application.TrainingGrounds.GetTrainingGrounds;

/// <summary>
/// Reads the training grounds sorted by name, for example to choose one for driving slots. A driving instructor sees
/// the grounds of their branch and the shared ones.
/// </summary>
/// <param name="BranchId">Only grounds of this branch.</param>
/// <param name="IsActive">Only grounds in use or only those no longer used.</param>
[RequireRoles(UserRole.Administrator, UserRole.DrivingInstructor)]
public sealed record GetTrainingGroundsQuery(Guid? BranchId = null, bool? IsActive = null) : IQuery<IReadOnlyList<TrainingGroundItem>>;
