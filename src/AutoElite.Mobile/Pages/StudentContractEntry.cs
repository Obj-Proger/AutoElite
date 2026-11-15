namespace AutoElite.Mobile.Pages;

/// <summary>How a contract of a student advances, on the page of the student.</summary>
public sealed class StudentContractEntry
{
    /// <summary>Gets the course, the category and the gearbox.</summary>
    public required string Course { get; init; }

    /// <summary>Gets the status and the dates of the training.</summary>
    public required string State { get; init; }

    /// <summary>Gets the practice hours driven of those of the course.</summary>
    public required string Practice { get; init; }

    /// <summary>Gets the share of the practice hours driven, from 0 to 1.</summary>
    public double PracticeProgress { get; init; }

    /// <summary>Gets the driving lessons completed, missed and coming, and the rating the student gives.</summary>
    public required string Driving { get; init; }

    /// <summary>Gets the group and the lessons attended of those held, or <see langword="null"/> without a group.</summary>
    public string? Group { get; init; }

    /// <summary>Gets a value indicating whether the student is in a group.</summary>
    public bool HasGroup => Group is not null;

    /// <summary>Gets the tests taken and passed.</summary>
    public required string Tests { get; init; }

    /// <summary>Gets the exams with their results.</summary>
    public IReadOnlyList<string> Exams { get; init; } = [];

    /// <summary>Gets a value indicating whether the student has exams.</summary>
    public bool HasExams => Exams.Count > 0;
}
