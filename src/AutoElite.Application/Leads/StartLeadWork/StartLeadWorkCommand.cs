namespace AutoElite.Application.Leads.StartLeadWork;

/// <summary>Takes a new lead into work (FR-2.3).</summary>
/// <param name="LeadId">The identifier of the lead.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record StartLeadWorkCommand(Guid LeadId) : ICommand;
