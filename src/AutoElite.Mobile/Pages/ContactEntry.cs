using System.Windows.Input;

namespace AutoElite.Mobile.Pages;

/// <summary>A person in a list to choose from, such as the people of a new chat or the students found by a manager.</summary>
public sealed class ContactEntry
{
    /// <summary>Gets the full name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets how the person works with the user, such as their driving instructor, or how to reach them.</summary>
    public required string Roles { get; init; }

    /// <summary>Gets the command that opens what the person is chosen for, such as the personal chat with them.</summary>
    public required ICommand OpenCommand { get; init; }
}
