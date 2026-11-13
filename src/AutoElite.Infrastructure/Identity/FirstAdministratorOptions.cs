using AutoElite.Application.Common.Localization;
using AutoElite.Domain.Enums;

namespace AutoElite.Infrastructure.Identity;

/// <summary>
/// The first administrator of a new school, from the optional <see cref="SectionName"/> section: the host invites
/// them by email when it starts while the school has no active administrator. No password is set here; the
/// administrator sets one from the invitation.
/// </summary>
public sealed class FirstAdministratorOptions
{
    /// <summary>The configuration section of the options.</summary>
    public const string SectionName = "FirstAdministrator";

    /// <summary>Gets or sets the email address; nobody is invited when it is not set.</summary>
    public string? Email { get; set; }

    /// <summary>Gets or sets the last name.</summary>
    public string? LastName { get; set; }

    /// <summary>Gets or sets the first name.</summary>
    public string? FirstName { get; set; }

    /// <summary>Gets or sets the language of the invitation, notifications and emails.</summary>
    public Language PreferredLanguage { get; set; } = SupportedLanguages.Default;
}
