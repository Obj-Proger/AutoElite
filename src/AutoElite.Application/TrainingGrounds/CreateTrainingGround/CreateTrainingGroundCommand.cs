namespace AutoElite.Application.TrainingGrounds.CreateTrainingGround;

/// <summary>Adds a training ground: a closed area or a starting point in the city (FR-6.2).</summary>
/// <param name="Name">The name.</param>
/// <param name="City">The city.</param>
/// <param name="Street">The street, building and other details.</param>
/// <param name="Description">Directions and what is available, if any.</param>
/// <param name="BranchId">The branch the ground belongs to; empty for a ground shared by all branches.</param>
[RequireRoles(UserRole.Administrator)]
public sealed record CreateTrainingGroundCommand(
    string? Name,
    string? City,
    string? Street,
    string? Description = null,
    Guid? BranchId = null) : ICommand<Guid>;
