using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.Contracts;

/// <summary>Reads contracts for queries.</summary>
public interface IContractQueries
{
    /// <summary>
    /// Reads one page of the contracts that match the filter and are inside the scope, the latest start first.
    /// </summary>
    /// <param name="filter">The conditions of the list.</param>
    /// <param name="scope">The branches the caller may see; contracts without a branch are in every scope.</param>
    /// <param name="page">The page to read.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<PagedList<ContractListItem>> GetListAsync(
        ContractListFilter filter, DataScope scope, PageRequest page, CancellationToken cancellationToken = default);

    /// <summary>Reads all contracts of a student, the latest start first; a student has only a few.</summary>
    /// <param name="studentId">The identifier of the student.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<ContractListItem>> GetOfStudentAsync(Guid studentId, CancellationToken cancellationToken = default);

    /// <summary>Reads the card of a contract.</summary>
    /// <param name="contractId">The identifier of the contract.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The card, or <see langword="null"/> if the contract does not exist.</returns>
    Task<ContractDetails?> GetDetailsAsync(Guid contractId, CancellationToken cancellationToken = default);
}
