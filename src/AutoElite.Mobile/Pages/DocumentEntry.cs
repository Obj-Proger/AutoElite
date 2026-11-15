using System.Windows.Input;

namespace AutoElite.Mobile.Pages;

/// <summary>A document on the page of the documents of a student.</summary>
public sealed class DocumentEntry
{
    /// <summary>Gets the name of the file.</summary>
    public required string Name { get; init; }

    /// <summary>Gets what the document is, its size, who added it and when.</summary>
    public required string Details { get; init; }

    /// <summary>Gets the command that opens the document.</summary>
    public required ICommand OpenCommand { get; init; }

    /// <summary>Gets the command that deletes the document, or <see langword="null"/> for one the user did not add.</summary>
    public ICommand? DeleteCommand { get; init; }

    /// <summary>Gets a value indicating whether the user can delete the document.</summary>
    public bool CanDelete => DeleteCommand is not null;
}
