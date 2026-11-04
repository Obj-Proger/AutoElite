using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.Contracts.GetContracts;

/// <summary>
/// Reads a page of the contract list for staff, the latest start first. Managers see the contracts of their branch
/// and those without a branch.
/// </summary>
/// <param name="Search">Text to find in the contract number or the student's name or email.</param>
/// <param name="Status">Only contracts in this status.</param>
/// <param name="CourseId">Only contracts for this course.</param>
/// <param name="BranchId">Only contracts of this branch.</param>
/// <param name="StudentId">Only contracts of this student.</param>
/// <param name="HasDebt">Only contracts with, or only without, an amount still owed.</param>
/// <param name="Page">The page number, starting from 1.</param>
/// <param name="PageSize">The number of contracts per page.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record GetContractsQuery(
    string? Search = null,
    ContractStatus? Status = null,
    Guid? CourseId = null,
    Guid? BranchId = null,
    Guid? StudentId = null,
    bool? HasDebt = null,
    int Page = 1,
    int PageSize = PageRequest.DefaultPageSize) : IQuery<PagedList<ContractListItem>>, IPagedQuery;
