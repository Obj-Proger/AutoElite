using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.Leads.GetLeads;

/// <summary>
/// Reads a page of the lead list, newest first. Managers see the leads of their branch and the common pool of
/// leads without a branch; to see only their own leads, they filter by their identifier.
/// </summary>
/// <param name="Search">Text to find in the contact name, phone or email.</param>
/// <param name="Status">Only leads in this status.</param>
/// <param name="Source">Only leads from this channel.</param>
/// <param name="ManagerId">Only leads of this manager.</param>
/// <param name="BranchId">Only leads of this branch.</param>
/// <param name="Page">The page number, starting from 1.</param>
/// <param name="PageSize">The number of leads per page.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record GetLeadsQuery(
    string? Search = null,
    LeadStatus? Status = null,
    LeadSource? Source = null,
    Guid? ManagerId = null,
    Guid? BranchId = null,
    int Page = 1,
    int PageSize = PageRequest.DefaultPageSize) : IQuery<PagedList<LeadListItem>>, IPagedQuery;
