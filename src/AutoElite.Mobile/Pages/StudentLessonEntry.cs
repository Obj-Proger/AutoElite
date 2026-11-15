namespace AutoElite.Mobile.Pages;

/// <summary>A driving lesson of a student with the instructor, on the page of the student.</summary>
public sealed class StudentLessonEntry
{
    /// <summary>Gets the day and the time.</summary>
    public required string Time { get; init; }

    /// <summary>Gets how the lesson went: coming, completed with its hours and grade, or missed.</summary>
    public required string Outcome { get; init; }

    /// <summary>Gets the note of the instructor, or <see langword="null"/>.</summary>
    public string? Note { get; init; }

    /// <summary>Gets a value indicating whether the lesson has a note.</summary>
    public bool HasNote => !string.IsNullOrEmpty(Note);
}
