using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The tasks the school gives the user: the open ones, the earliest due first, with the overdue ones marked, or those
/// done; from the API or, without a network, from the device. A task opens with its description and what to do with it.
/// </summary>
/// <param name="tasks">The tasks of the user.</param>
/// <param name="timeProvider">The clock.</param>
public sealed class TasksViewModel(StaffTasks tasks, TimeProvider timeProvider) : ViewModel
{
    /// <summary>Gets a value indicating whether the tasks done are shown, rather than the open ones.</summary>
    public bool ShowsDone
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(ShowsOpen));
        }
    }

    /// <summary>Gets a value indicating whether the open tasks are shown.</summary>
    public bool ShowsOpen => !ShowsDone;

    /// <summary>Gets the tasks shown.</summary>
    public IReadOnlyList<TaskEntry> Items
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets that there are no tasks, that they are from the device, or why they could not be read.</summary>
    public string? Notice
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasNotice));
        }
    }

    /// <summary>Gets a value indicating whether there is a notice.</summary>
    public bool HasNotice => Notice is not null;

    /// <summary>Gets the command that shows the open tasks.</summary>
    public Command ShowOpenCommand => field ??= new Command(async () => await ShowAsync(done: false));

    /// <summary>Gets the command that shows the tasks done.</summary>
    public Command ShowDoneCommand => field ??= new Command(async () => await ShowAsync(done: true));

    /// <summary>Reads the tasks shown, each time the page appears.</summary>
    /// <returns>A task that completes when the page shows them.</returns>
    public async Task LoadAsync()
    {
        var now = timeProvider.GetUtcNow();
        try
        {
            IReadOnlyList<StaffTaskListItem> items;
            DateTimeOffset? offlineSince;
            if (ShowsDone)
            {
                var done = await tasks.ReadMineAsync(StaffTaskStatus.Done, CancellationToken.None);
                items = [.. done.Value.OrderByDescending(task => task.DueAtUtc)];
                offlineSince = done.IsFromCache ? done.SavedAt : null;
            }
            else
            {
                (items, offlineSince) = await tasks.ReadOpenAsync(CancellationToken.None);
            }

            Items = [.. items.Where(task => task.Id is not null).Select(task => Entry(task, now))];
            Notice = offlineSince is { } savedAt
                ? AppTexts.Format("Tasks.Offline", savedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture))
                : Items.Count == 0 ? (ShowsDone ? AppTexts.Get("Tasks.NoneDone") : AppTexts.Get("Tasks.NoneOpen")) : null;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Notice = ProblemTexts.Of(exception);
        }
    }

    private static TaskEntry Entry(StaffTaskListItem task, DateTimeOffset now)
    {
        var taskId = task.Id!.Value;

        return new TaskEntry
        {
            Title = task.Title ?? string.Empty,
            Due = TaskTexts.DueOf(task.DueAtUtc, task.Recurrence),
            Status = TaskTexts.StatusOf(task.Status, task.DueAtUtc, now),
            IsOverdue = task.Status != StaffTaskStatus.Done && task.DueAtUtc < now,
            OpenCommand = new Command(async () => await Routes.OpenAsync(
                Routes.StaffTask, new Dictionary<string, object> { [TaskViewModel.TaskParameter] = taskId }))
        };
    }

    private async Task ShowAsync(bool done)
    {
        ShowsDone = done;
        Items = [];
        await LoadAsync();
    }
}
