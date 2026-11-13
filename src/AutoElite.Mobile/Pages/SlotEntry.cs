using System.Windows.Input;

namespace AutoElite.Mobile.Pages;

/// <summary>A free practice slot on the booking page, ready to show.</summary>
public sealed class SlotEntry
{
    /// <summary>Gets the day and the time of the slot, in the time zone of the device.</summary>
    public required string Time { get; init; }

    /// <summary>Gets the vehicle of the slot.</summary>
    public required string Vehicle { get; init; }

    /// <summary>Gets the instructor and the place where the lesson starts.</summary>
    public required string Details { get; init; }

    /// <summary>Gets the command that books the slot.</summary>
    public required ICommand BookCommand { get; init; }
}
