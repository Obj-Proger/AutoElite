using System.Windows.Input;

namespace AutoElite.Mobile.Pages;

/// <summary>A student of the group of an exam with their result, on the page of the exam.</summary>
public sealed class ExamMemberEntry
{
    /// <summary>Gets the full name of the student and the contract number.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the result, or that there is none yet.</summary>
    public required string Result { get; init; }

    /// <summary>Gets the comment of the result and whether it was corrected, or <see langword="null"/>.</summary>
    public string? Comment { get; init; }

    /// <summary>Gets a value indicating whether there is a comment.</summary>
    public bool HasComment => !string.IsNullOrEmpty(Comment);

    /// <summary>Gets what the button says: record or correct the result, or <see langword="null"/> without one.</summary>
    public string? ActionText { get; init; }

    /// <summary>Gets a value indicating whether the user records or corrects the result.</summary>
    public bool HasAction => ActionText is not null;

    /// <summary>Gets the command that records or corrects the result.</summary>
    public ICommand? Command { get; init; }
}
