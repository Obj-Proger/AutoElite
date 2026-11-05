namespace AutoElite.Application.Users;

/// <summary>A user's own profile, as shown on the profile page.</summary>
/// <param name="Id">The identifier of the user.</param>
/// <param name="LastName">The last name.</param>
/// <param name="FirstName">The first name.</param>
/// <param name="MiddleName">The middle name, if any.</param>
/// <param name="Email">The email address, which is also the sign-in name.</param>
/// <param name="Phone">The phone number in E.164 format, if any.</param>
/// <param name="PhotoDocumentId">The document with the profile photo, if any; anyone signed in can download it.</param>
/// <param name="BranchId">The branch of the user, if any.</param>
/// <param name="BranchName">The name of the branch, if any.</param>
/// <param name="PreferredLanguage">The language of notifications and emails.</param>
/// <param name="Roles">The roles the user holds.</param>
public sealed record UserProfile(
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
    IReadOnlyList<UserRole> Roles);
