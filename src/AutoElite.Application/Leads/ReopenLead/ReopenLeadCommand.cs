namespace AutoElite.Application.Leads.ReopenLead;

/// <summary>Returns a lost lead to work, for example when the person calls again.</summary>
/// <param name="LeadId">The identifier of the lead.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record ReopenLeadCommand(Guid LeadId) : ICommand;
