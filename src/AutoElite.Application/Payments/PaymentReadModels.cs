using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.Payments;

/// <summary>A payment as lists and histories show it.</summary>
/// <param name="Id">The identifier of the payment.</param>
/// <param name="ContractId">The contract.</param>
/// <param name="ContractNumber">The contract number.</param>
/// <param name="StudentId">The student.</param>
/// <param name="StudentName">The full name of the student.</param>
/// <param name="Amount">The amount.</param>
/// <param name="Currency">The ISO 4217 code of the currency.</param>
/// <param name="Method">How the payment was made.</param>
/// <param name="Purpose">What the payment is for.</param>
/// <param name="ExternalReference">The identifier in the bank or gateway, if any.</param>
/// <param name="Status">The status.</param>
/// <param name="RegisteredAtUtc">When the payment was recorded.</param>
/// <param name="RegisteredByUserId">The staff member who recorded it, or <see langword="null"/> for an online payment.</param>
/// <param name="RegisteredByName">The full name of that staff member, if any.</param>
/// <param name="ConfirmedAtUtc">When it was confirmed, if it was.</param>
/// <param name="RefundedAtUtc">When it was refunded, if it was.</param>
/// <param name="CancelledAtUtc">When it was cancelled, if it was.</param>
/// <param name="Reason">The reason of the refund or the cancellation, if any.</param>
public sealed record PaymentItem(
    Guid Id,
    Guid ContractId,
    string ContractNumber,
    Guid StudentId,
    string StudentName,
    decimal Amount,
    string Currency,
    PaymentMethod Method,
    string Purpose,
    string? ExternalReference,
    PaymentStatus Status,
    DateTime RegisteredAtUtc,
    Guid? RegisteredByUserId,
    string? RegisteredByName,
    DateTime? ConfirmedAtUtc,
    DateTime? RefundedAtUtc,
    DateTime? CancelledAtUtc,
    string? Reason);

/// <summary>The payments of one contract, with what is needed to check who may see them.</summary>
/// <param name="StudentId">The student of the contract.</param>
/// <param name="BranchId">The branch of the contract, if any.</param>
/// <param name="Payments">The payments, oldest first.</param>
public sealed record ContractPayments(Guid StudentId, Guid? BranchId, IReadOnlyList<PaymentItem> Payments);

/// <summary>The conditions of the payment report; a condition that is <see langword="null"/> does not filter.</summary>
/// <param name="From">Only payments recorded on or after this UTC day.</param>
/// <param name="To">Only payments recorded on or before this UTC day.</param>
/// <param name="Method">Only payments made this way.</param>
/// <param name="Status">Only payments in this status.</param>
/// <param name="RegisteredByUserId">Only payments recorded by this staff member.</param>
/// <param name="BranchId">Only payments of contracts of this branch.</param>
/// <param name="Search">Text to find in the contract number or the student's name, case-insensitively.</param>
public sealed record PaymentReportFilter(
    DateOnly? From,
    DateOnly? To,
    PaymentMethod? Method,
    PaymentStatus? Status,
    Guid? RegisteredByUserId,
    Guid? BranchId,
    string? Search);

/// <summary>The sum of the matching payments in one currency; amounts in different currencies are never added up.</summary>
/// <param name="Currency">The ISO 4217 code of the currency.</param>
/// <param name="Amount">The sum of the amounts.</param>
/// <param name="Count">The number of payments.</param>
public sealed record CurrencyTotal(string Currency, decimal Amount, int Count);

/// <summary>One page of the payment report with the totals of all matching payments.</summary>
/// <param name="Page">The payments of the page, the latest first.</param>
/// <param name="Totals">The totals per currency over every page, sorted by currency code.</param>
public sealed record PaymentReport(PagedList<PaymentItem> Page, IReadOnlyList<CurrencyTotal> Totals);
