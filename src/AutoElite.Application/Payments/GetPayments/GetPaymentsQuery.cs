using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.Payments.GetPayments;

/// <summary>
/// Reads the payments of the school for a period, the latest first, with totals per currency (FR-11.6). Managers see
/// the payments of the contracts of their branch and of contracts without a branch.
/// </summary>
/// <param name="From">The first UTC day of the period.</param>
/// <param name="To">The last UTC day of the period; not before <paramref name="From"/>.</param>
/// <param name="Method">Only payments made this way.</param>
/// <param name="Status">Only payments in this status, for example confirmed ones for the income.</param>
/// <param name="RegisteredByUserId">Only payments recorded by this staff member.</param>
/// <param name="BranchId">Only payments of contracts of this branch.</param>
/// <param name="Search">Text to find in the contract number or the student's name.</param>
/// <param name="Page">The page number, starting from 1.</param>
/// <param name="PageSize">The number of payments per page.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record GetPaymentsQuery(
    DateOnly? From = null,
    DateOnly? To = null,
    PaymentMethod? Method = null,
    PaymentStatus? Status = null,
    Guid? RegisteredByUserId = null,
    Guid? BranchId = null,
    string? Search = null,
    int Page = 1,
    int PageSize = PageRequest.DefaultPageSize) : IQuery<PaymentReport>, IPagedQuery;
