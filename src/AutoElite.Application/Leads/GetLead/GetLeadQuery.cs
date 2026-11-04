namespace AutoElite.Application.Leads.GetLead;

/// <summary>Reads the card of a lead with its notes and status history.</summary>
/// <param name="LeadId">The identifier of the lead.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record GetLeadQuery(Guid LeadId) : IQuery<LeadDetails>;
