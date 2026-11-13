using System.Windows.Input;

namespace AutoElite.Mobile.Pages;

/// <summary>A lesson in the schedule on the home page, ready to show.</summary>
public sealed class ScheduleEntry
{
    /// <summary>Gets the kind of the lesson: driving or theory.</summary>
    public required string Kind { get; init; }

    /// <summary>Gets what the lesson is: the vehicle of a driving lesson, the topic of a theory lesson.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the day and the time of the lesson, in the time zone of the device.</summary>
    public required string Time { get; init; }

    /// <summary>Gets the instructor and the place or the group.</summary>
    public required string Details { get; init; }

    /// <summary>Gets the state of the lesson when it is not simply ahead, such as cancelled or completed.</summary>
    public string? Status { get; init; }

    /// <summary>Gets a value indicating whether the lesson has a state to show.</summary>
    public bool HasStatus => Status is not null;

    /// <summary>Gets the command that cancels the booking of a driving lesson, when the student may cancel it.</summary>
    public ICommand? CancelCommand { get; init; }

    /// <summary>Gets a value indicating whether the booking may be cancelled.</summary>
    public bool CanCancel => CancelCommand is not null;

    /// <summary>Gets the start of the lesson, by which the schedule is ordered.</summary>
    internal DateTimeOffset Start { get; init; }
}
