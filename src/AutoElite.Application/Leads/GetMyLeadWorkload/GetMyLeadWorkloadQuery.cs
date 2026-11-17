namespace AutoElite.Application.Leads.GetMyLeadWorkload;

/// <summary>Reads how many leads the signed-in user works on and how many their limit allows.</summary>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record GetMyLeadWorkloadQuery : IQuery<LeadWorkload>;
