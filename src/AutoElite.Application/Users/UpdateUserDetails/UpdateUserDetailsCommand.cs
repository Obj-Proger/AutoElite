namespace AutoElite.Application.Users.UpdateUserDetails;

/// <summary>
/// Corrects the name and phone of another user, for example a typo in a student's name. Managers correct only
/// students of their branch. The email address is changed only by its owner, because the new one must be confirmed.
/// </summary>
/// <param name="UserId">The identifier of the user.</param>
/// <param name="LastName">The last name. Required.</param>
/// <param name="FirstName">The first name. Required.</param>
/// <param name="MiddleName">The middle name; empty removes it.</param>
/// <param name="Phone">The phone number in E.164 format; empty removes it.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record UpdateUserDetailsCommand(
    Guid UserId,
    string? LastName,
    string? FirstName,
    string? MiddleName,
    string? Phone) : ICommand;
