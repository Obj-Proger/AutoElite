using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.Payments;

/// <summary>Reads payments for queries.</summary>
public interface IPaymentQueries
{
    /// <summary>Reads the payments of a contract.</summary>
    /// <param name="contractId">The identifier of the contract.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The payments, or <see langword="null"/> if the contract does not exist.</returns>
    Task<ContractPayments?> GetOfContractAsync(Guid contractId, CancellationToken cancellationToken = default);

    /// <summary>Reads the payments of all contracts of a student, the latest first.</summary>
    /// <param name="studentId">The identifier of the student.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<PaymentItem>> GetOfStudentAsync(Guid studentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads one page of the payments that match the filter and whose contracts are inside the scope, the latest
    /// first, with the totals per currency computed by the database over all matching payments (NFR-4.2).
    /// </summary>
    /// <param name="filter">The conditions of the report.</param>
    /// <param name="scope">The branches the caller may see; contracts without a branch are in every scope.</param>
    /// <param name="page">The page to read.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<PaymentReport> GetReportAsync(
        PaymentReportFilter filter, DataScope scope, PageRequest page, CancellationToken cancellationToken = default);
}
