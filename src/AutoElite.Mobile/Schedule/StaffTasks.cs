using System.Globalization;
using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;

namespace AutoElite.Mobile.Schedule;

/// <summary>
/// The tasks the school gives a member of its staff, an instructor among them: the open ones, the earliest due first,
/// or those done, read from the API or, without a network, from the device. The assignee starts a task, marks it done
/// and opens it again; these need the network.
/// </summary>
/// <param name="cache">What the user last read.</param>
/// <param name="createApi">Creates the client of the API.</param>
public sealed class StaffTasks(OfflineCache cache, Func<AutoEliteApiClient> createApi)
{
    // The tasks read at once: enough for the open tasks of one person.
    private const int PageSize = 100;

    /// <summary>Reads the tasks of the user with a status, the earliest due first.</summary>
    /// <param name="status">The status.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The tasks.</returns>
    public Task<Cached<IReadOnlyList<StaffTaskListItem>>> ReadMineAsync(StaffTaskStatus status, CancellationToken cancellationToken) =>
        cache.GetListAsync(
            string.Create(CultureInfo.InvariantCulture, $"me/staff-tasks/{status}"),
            async token => (await createApi().Api.V1.Me.StaffTasks.GetAsync(
                request =>
                {
                    request.QueryParameters.Status = status;
                    request.QueryParameters.PageSize = PageSize;
                },
                token))?.Items,
            StaffTaskListItem.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Reads the open tasks of the user: new ones and those in progress, the earliest due first.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The tasks, and when they were read if they come from the device.</returns>
    public async Task<(IReadOnlyList<StaffTaskListItem> Tasks, DateTimeOffset? OfflineSince)> ReadOpenAsync(
        CancellationToken cancellationToken)
    {
        var fresh = await ReadMineAsync(StaffTaskStatus.New, cancellationToken);
        var started = await ReadMineAsync(StaffTaskStatus.InProgress, cancellationToken);
        DateTimeOffset? offlineSince = (fresh.IsFromCache, started.IsFromCache) switch
        {
            (true, true) => fresh.SavedAt < started.SavedAt ? fresh.SavedAt : started.SavedAt,
            (true, false) => fresh.SavedAt,
            (false, true) => started.SavedAt,
            _ => null
        };

        IReadOnlyList<StaffTaskListItem> open = [.. fresh.Value.Concat(started.Value).OrderBy(task => task.DueAtUtc)];

        return (open, offlineSince);
    }

    /// <summary>Reads a task with its description.</summary>
    /// <param name="taskId">The task.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The task.</returns>
    public Task<Cached<StaffTaskDetails>> ReadAsync(Guid taskId, CancellationToken cancellationToken) =>
        cache.GetAsync(
            string.Create(CultureInfo.InvariantCulture, $"staff-tasks/{taskId:N}"),
            token => createApi().Api.V1.StaffTasks[taskId].GetAsync(cancellationToken: token),
            StaffTaskDetails.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Starts a new task.</summary>
    /// <param name="taskId">The task.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the task is started.</returns>
    public Task StartAsync(Guid taskId, CancellationToken cancellationToken) =>
        createApi().Api.V1.StaffTasks[taskId].Start.PostAsync(cancellationToken: cancellationToken);

    /// <summary>Marks a task done; a recurring task comes back with its next deadline.</summary>
    /// <param name="taskId">The task.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the task is done.</returns>
    public Task CompleteAsync(Guid taskId, CancellationToken cancellationToken) =>
        createApi().Api.V1.StaffTasks[taskId].Complete.PostAsync(cancellationToken: cancellationToken);

    /// <summary>Opens a task that was done again.</summary>
    /// <param name="taskId">The task.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the task is open.</returns>
    public Task ReopenAsync(Guid taskId, CancellationToken cancellationToken) =>
        createApi().Api.V1.StaffTasks[taskId].Reopen.PostAsync(cancellationToken: cancellationToken);
}
