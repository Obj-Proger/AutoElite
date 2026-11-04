using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.StudyGroups;

/// <summary>Reads study groups for queries.</summary>
public interface IStudyGroupQueries
{
    /// <summary>Reads one page of the groups that match the filter and are inside the scope, the latest start first.</summary>
    /// <param name="filter">The conditions of the list.</param>
    /// <param name="scope">The branches the caller may see; groups without a branch are in every scope.</param>
    /// <param name="page">The page to read.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<PagedList<StudyGroupListItem>> GetListAsync(
        StudyGroupListFilter filter, DataScope scope, PageRequest page, CancellationToken cancellationToken = default);

    /// <summary>Reads the card of a group.</summary>
    /// <param name="groupId">The identifier of the group.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The card, or <see langword="null"/> if the group does not exist.</returns>
    Task<StudyGroupDetails?> GetDetailsAsync(Guid groupId, CancellationToken cancellationToken = default);
}
