namespace AutoElite.Domain.Enums;

/// <summary>
/// The status of a staff task (FR-12.2): <see cref="New"/> to <see cref="InProgress"/> or <see cref="Done"/>,
/// <see cref="InProgress"/> to <see cref="Done"/>, and a done task can be reopened to <see cref="InProgress"/>.
/// </summary>
public enum StaffTaskStatus
{
    /// <summary>Assigned but not started.</summary>
    New = 1,

    /// <summary>The assignee is working on it.</summary>
    InProgress = 2,

    /// <summary>Finished.</summary>
    Done = 3
}
