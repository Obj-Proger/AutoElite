namespace AutoElite.Application.TrainingGrounds.UpdateTrainingGround;

/// <summary>Changes the details of a ground in use.</summary>
/// <param name="GroundId">The identifier of the ground.</param>
/// <param name="Name">The name.</param>
/// <param name="City">The city.</param>
/// <param name="Street">The street, building and other details.</param>
/// <param name="Description">Directions and what is available; empty removes it.</param>
/// <param name="BranchId">The branch the ground belongs to; empty for a ground shared by all branches.</param>
[RequireRoles(UserRole.Administrator)]
public sealed record UpdateTrainingGroundCommand(
    Guid GroundId,
    string? Name,
    string? City,
    string? Street,
    string? Description = null,
    Guid? BranchId = null) : ICommand;
