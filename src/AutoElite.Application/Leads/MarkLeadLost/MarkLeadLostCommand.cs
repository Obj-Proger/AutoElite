namespace AutoElite.Application.Leads.MarkLeadLost;

/// <summary>Records that an open lead will not become a student, with the reason for the loss statistics (FR-2.3).</summary>
/// <param name="LeadId">The identifier of the lead.</param>
/// <param name="Reason">Why the lead was lost. Required.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record MarkLeadLostCommand(Guid LeadId, string? Reason) : ICommand;
