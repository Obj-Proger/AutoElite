using System.Windows.Input;
using AutoElite.Mobile.Controls;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// A line of figures: in the summary of the school a source of leads, an instructor or a staff member; in the practice
/// of a student a topic, which opens its practice.
/// </summary>
public sealed class SummaryRow
{
    /// <summary>Gets the icon of the row.</summary>
    public required string Icon { get; init; }

    /// <summary>Gets the tone of the row: danger when it calls for attention.</summary>
    public StatusTone Tone { get; init; }

    /// <summary>Gets the name of the source or the person.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the figures of the row.</summary>
    public required string Subtitle { get; init; }

    /// <summary>Gets the main figure, at the end of the row.</summary>
    public required string Trailing { get; init; }

    /// <summary>Gets what the main figure is.</summary>
    public string? Detail { get; init; }

    /// <summary>Gets what tapping the row does, if anything.</summary>
    public ICommand? Command { get; init; }
}
