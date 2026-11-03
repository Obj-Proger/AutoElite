namespace AutoElite.Domain.Enums;

/// <summary>The status of a test attempt. <see cref="Passed"/> and <see cref="Failed"/> are final.</summary>
public enum TestSessionStatus
{
    /// <summary>The student is answering.</summary>
    InProgress = 1,

    /// <summary>Every question was answered within the rules.</summary>
    Passed = 2,

    /// <summary>The rules were broken or the time ran out; see <see cref="TestFailureReason"/>.</summary>
    Failed = 3
}
