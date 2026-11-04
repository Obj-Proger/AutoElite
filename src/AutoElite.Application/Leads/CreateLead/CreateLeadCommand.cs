namespace AutoElite.Application.Leads.CreateLead;

/// <summary>
/// Records a lead that came by phone, in person or through another channel (FR-2.1). A manager's lead goes to the
/// manager's branch and to the manager unless others are given.
/// </summary>
/// <param name="ContactName">The name the person gave.</param>
/// <param name="Phone">The phone number in E.164 format, if given.</param>
/// <param name="Email">The email address, if given; a phone or an email is required.</param>
/// <param name="Source">The channel through which the lead arrived.</param>
/// <param name="DesiredCategory">The licence category of interest, if known.</param>
/// <param name="Comment">A free-text comment, if any.</param>
/// <param name="BranchId">The branch handling the lead; for a manager, the manager's branch if not given.</param>
/// <param name="ManagerId">The responsible manager; for a manager, the manager if not given.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record CreateLeadCommand(
    string? ContactName,
    string? Phone,
    string? Email,
    LeadSource Source,
    LicenseCategory? DesiredCategory,
    string? Comment,
    Guid? BranchId,
    Guid? ManagerId) : ICommand<Guid>;
