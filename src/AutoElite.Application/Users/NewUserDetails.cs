namespace AutoElite.Application.Users;

/// <summary>The details of a user to register, as given in a command.</summary>
/// <param name="LastName">The last name.</param>
/// <param name="FirstName">The first name.</param>
/// <param name="MiddleName">The middle name, if any.</param>
/// <param name="Email">The email address, which is also the sign-in name.</param>
/// <param name="Phone">The phone number, if any.</param>
/// <param name="BranchId">The branch, if any; the caller checks it.</param>
/// <param name="PreferredLanguage">The language of the invitation, notifications and emails.</param>
internal sealed record NewUserDetails(
    string? LastName,
    string? FirstName,
    string? MiddleName,
    string? Email,
    string? Phone,
    Guid? BranchId,
    Language PreferredLanguage);
