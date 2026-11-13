using System.Windows.Input;

namespace AutoElite.Mobile.Pages;

/// <summary>A template to start a practice test from, or an attempt of the student, on the tests page.</summary>
public sealed class TestEntry
{
    /// <summary>Gets the name of the template.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the rules of the template, or when the attempt was taken and how it ended.</summary>
    public required string Details { get; init; }

    /// <summary>Gets the text of the button: start, continue or review.</summary>
    public required string ActionText { get; init; }

    /// <summary>Gets what the button does.</summary>
    public required ICommand Command { get; init; }
}
