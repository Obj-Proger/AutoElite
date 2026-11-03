namespace AutoElite.Domain.Enums;

/// <summary>
/// The status of a theory lesson: <see cref="Scheduled"/> to <see cref="Completed"/> (once every student has an
/// attendance mark) or to <see cref="Cancelled"/>.
/// </summary>
public enum TheoryLessonStatus
{
    /// <summary>Planned; attendance may be partly marked.</summary>
    Scheduled = 1,

    /// <summary>Every current member of the group has an attendance mark.</summary>
    Completed = 2,

    /// <summary>Cancelled with a reason; final.</summary>
    Cancelled = 3
}
