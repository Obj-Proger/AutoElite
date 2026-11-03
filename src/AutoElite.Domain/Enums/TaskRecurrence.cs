namespace AutoElite.Domain.Enums;

/// <summary>How often a staff task repeats (FR-12.3).</summary>
public enum TaskRecurrence
{
    /// <summary>The task does not repeat.</summary>
    None = 1,

    /// <summary>Every day.</summary>
    Daily = 2,

    /// <summary>Every week.</summary>
    Weekly = 3,

    /// <summary>Every month.</summary>
    Monthly = 4
}
