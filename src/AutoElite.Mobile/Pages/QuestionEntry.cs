using System.Windows.Input;

namespace AutoElite.Mobile.Pages;

/// <summary>A question on the page of the questions of the school.</summary>
public sealed class QuestionEntry
{
    /// <summary>Gets the question.</summary>
    public required string Text { get; init; }

    /// <summary>Gets the topic, the course and the categories.</summary>
    public required string Details { get; init; }

    /// <summary>Gets that the question has an illustration or is out of new tests, or empty.</summary>
    public required string Status { get; init; }

    /// <summary>Gets a value indicating whether new tests use the question.</summary>
    public bool IsActive { get; init; }

    /// <summary>Gets the command that opens the question to correct it.</summary>
    public required ICommand OpenCommand { get; init; }
}
