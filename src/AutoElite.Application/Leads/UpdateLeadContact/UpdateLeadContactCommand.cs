namespace AutoElite.Application.Leads.UpdateLeadContact;

/// <summary>Replaces the contact details of an open lead; empty optional values clear them.</summary>
/// <param name="LeadId">The identifier of the lead.</param>
/// <param name="ContactName">The name of the person.</param>
/// <param name="Phone">The phone number in E.164 format, if any.</param>
/// <param name="Email">The email address, if any; a phone or an email must remain.</param>
/// <param name="DesiredCategory">The licence category of interest, if known.</param>
/// <param name="Comment">A free-text comment, if any.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record UpdateLeadContactCommand(
    Guid LeadId,
    string? ContactName,
    string? Phone,
    string? Email,
    LicenseCategory? DesiredCategory,
    string? Comment) : ICommand;
