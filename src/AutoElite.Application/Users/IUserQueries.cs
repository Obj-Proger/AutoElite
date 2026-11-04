using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.Users;

/// <summary>
/// Reads users for queries. The infrastructure implements it with projections straight from the database, without
/// loading aggregates.
/// </summary>
public interface IUserQueries
{
    /// <summary>Reads the profile of a user, active or not.</summary>
    /// <param name="userId">The identifier of the user.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The profile, or <see langword="null"/> if the user does not exist.</returns>
    Task<UserProfile?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Reads the card of a user, active or not.</summary>
    /// <param name="userId">The identifier of the user.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The card, or <see langword="null"/> if the user does not exist.</returns>
    Task<UserDetails?> GetDetailsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads one page of the users that match the filter and are inside the scope, sorted by last, first and middle
    /// name and then by email.
    /// </summary>
    /// <param name="filter">The conditions of the list.</param>
    /// <param name="scope">The branches the caller may see; users without a branch are in every scope.</param>
    /// <param name="page">The page to read.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<PagedList<UserListItem>> GetListAsync(
        UserListFilter filter, DataScope scope, PageRequest page, CancellationToken cancellationToken = default);
}
