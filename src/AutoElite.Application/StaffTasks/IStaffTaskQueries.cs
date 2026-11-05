using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.StaffTasks;

/// <summary>Reads staff tasks for queries.</summary>
public interface IStaffTaskQueries
{
    /// <summary>
    /// Reads one page of the tasks that match the filter and whose assignee is inside the scope, the nearest deadline
    /// first.
    /// </summary>
    /// <param name="filter">The conditions of the list.</param>
    /// <param name="scope">The branches whose staff's tasks the caller may see; staff without a branch are in every scope.</param>
    /// <param name="page">The page to read.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<PagedList<StaffTaskListItem>> GetListAsync(
        StaffTaskListFilter filter, DataScope scope, PageRequest page, CancellationToken cancellationToken = default);

    /// <summary>Reads the card of a task.</summary>
    /// <param name="taskId">The identifier of the task.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The card, or <see langword="null"/> if the task does not exist.</returns>
    Task<StaffTaskDetails?> GetDetailsAsync(Guid taskId, CancellationToken cancellationToken = default);
}
