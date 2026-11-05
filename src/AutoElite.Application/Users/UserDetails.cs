namespace AutoElite.Application.Users;

/// <summary>The card of a user, as staff see it.</summary>
/// <param name="Id">The identifier of the user.</param>
/// <param name="LastName">The last name.</param>
/// <param name="FirstName">The first name.</param>
/// <param name="MiddleName">The middle name, if any.</param>
/// <param name="Email">The email address.</param>
/// <param name="Phone">The phone number, if any.</param>
/// <param name="PhotoDocumentId">The document with the profile photo, if any; anyone signed in can download it.</param>
/// <param name="BranchId">The branch of the user, if any.</param>
/// <param name="BranchName">The name of the branch, if any.</param>
/// <param name="PreferredLanguage">The language of notifications and emails.</param>
/// <param name="IsActive">Whether the user is active.</param>
/// <param name="HasAcceptedInvitation">Whether the user has set a password.</param>
/// <param name="CreatedAtUtc">When the user was invited.</param>
/// <param name="Roles">The roles the user holds.</param>
public sealed record UserDetails(
    Guid Id,
    string LastName,
    string FirstName,
    string? MiddleName,
    string Email,
    string? Phone,
    Guid? PhotoDocumentId,
    Guid? BranchId,
    string? BranchName,
    Language PreferredLanguage,
    bool IsActive,
    bool HasAcceptedInvitation,
    DateTime CreatedAtUtc,
    IReadOnlyList<UserRole> Roles);
