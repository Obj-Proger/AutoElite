using System.Windows.Input;

namespace AutoElite.Mobile.Pages;

/// <summary>A person on the page of a new chat.</summary>
public sealed class ContactEntry
{
    /// <summary>Gets the full name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets how the person works with the user, such as their driving instructor.</summary>
    public required string Roles { get; init; }

    /// <summary>Gets the initials, shown in place of a photo.</summary>
    public required string Initials { get; init; }

    /// <summary>Gets the command that opens the personal chat with the person.</summary>
    public required ICommand OpenCommand { get; init; }
}
