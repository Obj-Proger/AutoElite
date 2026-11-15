using System.Windows.Input;

namespace AutoElite.Mobile.Pages;

/// <summary>A file of a message, or one chosen to be sent with the next message.</summary>
public sealed class FileEntry
{
    /// <summary>Gets the name of the file.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the size of the file, or that it is being attached.</summary>
    public required string Details { get; init; }

    /// <summary>Gets the command that opens the file, or removes it from the next message.</summary>
    public required ICommand Command { get; init; }
}
