namespace AutoElite.Application.Users.UpdateMyProfile;

/// <summary>
/// Changes the name, phone and language of the signed-in user. The email address is changed separately, because the
/// new one must be confirmed.
/// </summary>
/// <param name="LastName">The last name. Required.</param>
/// <param name="FirstName">The first name. Required.</param>
/// <param name="MiddleName">The middle name; empty removes it.</param>
/// <param name="Phone">The phone number in E.164 format; empty removes it.</param>
/// <param name="PreferredLanguage">The language of notifications and emails.</param>
public sealed record UpdateMyProfileCommand(
    string? LastName,
    string? FirstName,
    string? MiddleName,
    string? Phone,
    Language PreferredLanguage) : ICommand;
