namespace AutoElite.Domain.Enums;

/// <summary>The status of an exam. <see cref="Cancelled"/> is final.</summary>
public enum ExamStatus
{
    /// <summary>Scheduled; results are recorded once it has started.</summary>
    Scheduled = 1,

    /// <summary>Cancelled with a reason before any result was recorded.</summary>
    Cancelled = 2
}
