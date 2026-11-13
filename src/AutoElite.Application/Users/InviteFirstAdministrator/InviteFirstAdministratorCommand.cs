using AutoElite.Application.Common.Localization;

namespace AutoElite.Application.Users.InviteFirstAdministrator;

/// <summary>
/// Invites the first administrator of the school, whom no one else can invite: while the school has no active
/// administrator, creates the user with the administrator role and sends the invitation to set a password; once it
/// has one, changes nothing. Only the system sends it, when the host starts with the first administrator in its
/// settings, so that a new school can be set up without a password in any configuration.
/// </summary>
/// <param name="LastName">The last name. Required.</param>
/// <param name="FirstName">The first name. Required.</param>
/// <param name="Email">The email address, unique among all users; it is also the sign-in name.</param>
/// <param name="PreferredLanguage">The language of the invitation, notifications and emails.</param>
[SystemRequest]
public sealed record InviteFirstAdministratorCommand(
    string? LastName,
    string? FirstName,
    string? Email,
    Language PreferredLanguage = SupportedLanguages.Default) : ICommand<Guid?>;
