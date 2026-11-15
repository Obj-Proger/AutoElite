using System.Windows.Input;

namespace AutoElite.Mobile.Pages;

/// <summary>A task of the staff on the page of tasks.</summary>
public sealed class TaskEntry
{
    /// <summary>Gets the title of the task.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the deadline and how often the task comes back.</summary>
    public required string Due { get; init; }

    /// <summary>Gets the status, and that the task is overdue.</summary>
    public required string Status { get; init; }

    /// <summary>Gets a value indicating whether the deadline has passed for an open task.</summary>
    public bool IsOverdue { get; init; }

    /// <summary>Gets the command that opens the task.</summary>
    public required ICommand OpenCommand { get; init; }
}
