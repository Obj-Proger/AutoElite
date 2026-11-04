using AutoElite.Application.Common.Localization;

namespace AutoElite.Application.Users.InviteUser;

/// <summary>
/// Creates a user with the given roles and sends an invitation to set a password (FR-1.1). Administrators invite
/// anyone; managers invite only students, into their own branch unless another branch in their scope is given.
/// </summary>
/// <param name="LastName">The last name. Required.</param>
/// <param name="FirstName">The first name. Required.</param>
/// <param name="MiddleName">The middle name, if any.</param>
/// <param name="Email">The email address, unique among all users; it is also the sign-in name.</param>
/// <param name="Phone">The phone number in E.164 format, if any.</param>
/// <param name="BranchId">The branch of the user; for a manager's invitation, the manager's branch if not given.</param>
/// <param name="Roles">The roles to grant, at least one.</param>
/// <param name="PreferredLanguage">The language of the invitation, notifications and emails.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record InviteUserCommand(
    string? LastName,
    string? FirstName,
    string? MiddleName,
    string? Email,
    string? Phone,
    Guid? BranchId,
    IReadOnlyList<UserRole>? Roles,
    Language PreferredLanguage = SupportedLanguages.Default) : ICommand<Guid>;
