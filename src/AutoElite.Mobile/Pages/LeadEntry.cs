using System.Windows.Input;

namespace AutoElite.Mobile.Pages;

/// <summary>A lead in the list of a manager.</summary>
public sealed class LeadEntry
{
    /// <summary>Gets the name the person gave.</summary>
    public required string Name { get; init; }

    /// <summary>Gets how to reach the person.</summary>
    public required string Contacts { get; init; }

    /// <summary>Gets where the lead came from, the licence category wanted and when it arrived.</summary>
    public required string Details { get; init; }

    /// <summary>Gets the command that opens the lead.</summary>
    public required ICommand OpenCommand { get; init; }
}
