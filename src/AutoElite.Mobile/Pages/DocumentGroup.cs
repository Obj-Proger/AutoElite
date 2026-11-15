namespace AutoElite.Mobile.Pages;

/// <summary>The documents of a contract, on the page of the documents of a student.</summary>
public sealed class DocumentGroup
{
    /// <summary>Gets the contract: its number and course.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the documents.</summary>
    public required IReadOnlyList<DocumentEntry> Documents { get; init; }
}
