namespace AutoElite.Application.Leads.AssignLeadManager;

/// <summary>
/// Makes a manager responsible for an open lead (FR-2.4). The manager must work for the whole school or for the
/// lead's branch; the manager is notified.
/// </summary>
/// <param name="LeadId">The identifier of the lead.</param>
/// <param name="ManagerId">The identifier of the manager.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record AssignLeadManagerCommand(Guid LeadId, Guid ManagerId) : ICommand;
