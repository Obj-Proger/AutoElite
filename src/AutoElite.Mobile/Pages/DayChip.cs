using System.Windows.Input;

namespace AutoElite.Mobile.Pages;

/// <summary>A day in the strip of the week above the schedule.</summary>
public sealed class DayChip
{
    /// <summary>Gets the day.</summary>
    public required DateOnly Date { get; init; }

    /// <summary>Gets the short name of the day of the week, such as "Mon".</summary>
    public required string Name { get; init; }

    /// <summary>Gets the number of the day in its month.</summary>
    public required string Number { get; init; }

    /// <summary>Gets a value indicating whether the day is the one shown.</summary>
    public bool IsSelected { get; init; }

    /// <summary>Gets a value indicating whether the day is today.</summary>
    public bool IsToday { get; init; }

    /// <summary>Gets a value indicating whether the day has lessons.</summary>
    public bool HasLessons { get; init; }

    /// <summary>Gets the command that shows the day.</summary>
    public required ICommand SelectCommand { get; init; }
}
