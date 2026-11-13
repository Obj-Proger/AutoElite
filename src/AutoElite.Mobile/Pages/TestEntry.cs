using System.Windows.Input;

namespace AutoElite.Mobile.Pages;

/// <summary>An exam, a template to start a practice test from, or an attempt of the student, on the tests page.</summary>
public sealed class TestEntry
{
    /// <summary>Gets the name of the template.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the rules of the template, or when the attempt was taken and how it ended.</summary>
    public required string Details { get; init; }

    /// <summary>Gets the text of the button: start, continue or review; none when there is nothing to do yet.</summary>
    public string? ActionText { get; init; }

    /// <summary>Gets what the button does.</summary>
    public ICommand? Command { get; init; }

    /// <summary>Gets a value indicating whether there is something to do.</summary>
    public bool HasAction => Command is not null;
}
