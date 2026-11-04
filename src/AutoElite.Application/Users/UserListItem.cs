namespace AutoElite.Application.Users;

/// <summary>A row of the user list.</summary>
/// <param name="Id">The identifier of the user.</param>
/// <param name="LastName">The last name.</param>
/// <param name="FirstName">The first name.</param>
/// <param name="MiddleName">The middle name, if any.</param>
/// <param name="Email">The email address.</param>
/// <param name="Phone">The phone number, if any.</param>
/// <param name="BranchId">The branch of the user, if any.</param>
/// <param name="BranchName">The name of the branch, if any.</param>
/// <param name="IsActive">Whether the user is active.</param>
/// <param name="HasAcceptedInvitation">Whether the user has set a password, so the invitation needs no resending.</param>
/// <param name="Roles">The roles the user holds.</param>
public sealed record UserListItem(
    Guid Id,
    string LastName,
    string FirstName,
    string? MiddleName,
    string Email,
    string? Phone,
    Guid? BranchId,
    string? BranchName,
    bool IsActive,
    bool HasAcceptedInvitation,
    IReadOnlyList<UserRole> Roles);
