namespace AutoElite.Application.TrainingGrounds;

/// <summary>A training ground where practical lessons take place.</summary>
/// <param name="Id">The identifier of the ground.</param>
/// <param name="Name">The name.</param>
/// <param name="City">The city of the address.</param>
/// <param name="Street">The street, building and other details of the address.</param>
/// <param name="Description">Directions and what is available, if recorded.</param>
/// <param name="BranchId">The branch, or <see langword="null"/> if the ground is shared.</param>
/// <param name="BranchName">The name of the branch, if any.</param>
/// <param name="IsActive">Whether the ground is in use.</param>
public sealed record TrainingGroundItem(
    Guid Id,
    string Name,
    string City,
    string Street,
    string? Description,
    Guid? BranchId,
    string? BranchName,
    bool IsActive);
