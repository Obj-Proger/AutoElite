using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// A task the school gave the user: its description, deadline and who set it. The user starts a new task, marks one
/// done, after which a recurring task comes back with its next deadline, and opens one done again; these need the
/// network.
/// </summary>
public sealed class TaskViewModel : ViewModel, IQueryAttributable
{
    /// <summary>The navigation parameter that carries the identifier of the task.</summary>
    public const string TaskParameter = "task";

    private readonly StaffTasks _tasks;
    private readonly TimeProvider _timeProvider;
    private Guid _taskId;

    /// <summary>Initializes a new instance of the <see cref="TaskViewModel"/> class.</summary>
    /// <param name="tasks">The tasks of the user.</param>
    /// <param name="timeProvider">The clock.</param>
    public TaskViewModel(StaffTasks tasks, TimeProvider timeProvider)
    {
        _tasks = tasks;
        _timeProvider = timeProvider;
        StartCommand = new Command(async () => await ChangeAsync(_tasks.StartAsync), () => CanStart && !IsBusy);
        CompleteCommand = new Command(async () => await ChangeAsync(_tasks.CompleteAsync), () => CanComplete && !IsBusy);
        ReopenCommand = new Command(async () => await ChangeAsync(_tasks.ReopenAsync), () => CanReopen && !IsBusy);
    }

    /// <summary>Gets the title of the task.</summary>
    public string Title
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the description, or empty.</summary>
    public string Description
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasDescription));
        }
    } = string.Empty;

    /// <summary>Gets a value indicating whether the task has a description.</summary>
    public bool HasDescription => Description.Length > 0;

    /// <summary>Gets the deadline and how often the task comes back.</summary>
    public string Due
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the status, and that the task is overdue.</summary>
    public string Status
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets who set the task and when, and when it was done.</summary>
    public string Origin
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets a value indicating whether the task is new and can be started.</summary>
    public bool CanStart
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets a value indicating whether the task is open and can be marked done.</summary>
    public bool CanComplete
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets a value indicating whether the task is done and can be opened again.</summary>
    public bool CanReopen
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets that the task is from the device, why a change failed, or <see langword="null"/>.</summary>
    public string? Message
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasMessage));
        }
    }

    /// <summary>Gets a value indicating whether there is a message.</summary>
    public bool HasMessage => Message is not null;

    /// <summary>Gets a value indicating whether a change is being sent.</summary>
    public bool IsBusy
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                RefreshCommands();
        }
    }

    /// <summary>Gets the command that starts the task.</summary>
    public Command StartCommand { get; }

    /// <summary>Gets the command that marks the task done.</summary>
    public Command CompleteCommand { get; }

    /// <summary>Gets the command that opens the task again.</summary>
    public Command ReopenCommand { get; }

    /// <inheritdoc />
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.TryGetValue(TaskParameter, out var value) && value is Guid taskId)
            _taskId = taskId;
    }

    /// <summary>Reads the task.</summary>
    /// <returns>A task that completes when the page shows it.</returns>
    public async Task LoadAsync()
    {
        try
        {
            var task = await _tasks.ReadAsync(_taskId, CancellationToken.None);
            Show(task.Value);
            Message = task.IsFromCache
                ? AppTexts.Format("Tasks.Offline", task.SavedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture))
                : null;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Message = ProblemTexts.Of(exception);
        }
    }

    private void Show(StaffTaskDetails task)
    {
        var culture = CultureInfo.CurrentCulture;
        Title = task.Title ?? string.Empty;
        Description = task.Description ?? string.Empty;
        Due = TaskTexts.DueOf(task.DueAtUtc, task.Recurrence);
        Status = TaskTexts.StatusOf(task.Status, task.DueAtUtc, _timeProvider.GetUtcNow());
        Origin = LessonTexts.DetailsOf(
            task.CreatedAtUtc is { } created
                ? AppTexts.Format("Task.Created", created.ToLocalTime().ToString("g", culture), task.CreatedByName ?? string.Empty)
                : null,
            task.CompletedAtUtc is { } completed
                ? AppTexts.Format("Task.Completed", completed.ToLocalTime().ToString("g", culture))
                : null);
        CanStart = task.Status == StaffTaskStatus.New;
        CanComplete = task.Status is StaffTaskStatus.New or StaffTaskStatus.InProgress;
        CanReopen = task.Status == StaffTaskStatus.Done;
        RefreshCommands();
    }

    private void RefreshCommands()
    {
        StartCommand.ChangeCanExecute();
        CompleteCommand.ChangeCanExecute();
        ReopenCommand.ChangeCanExecute();
    }

    private async Task ChangeAsync(Func<Guid, CancellationToken, Task> change)
    {
        IsBusy = true;
        Message = null;
        try
        {
            await change(_taskId, CancellationToken.None);
            await LoadAsync();
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            Message = ProblemTexts.Of(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
