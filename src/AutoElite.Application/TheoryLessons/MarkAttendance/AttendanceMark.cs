namespace AutoElite.Application.TheoryLessons.MarkAttendance;

/// <summary>The attendance of one student at a lesson.</summary>
/// <param name="ContractId">The contract of the student.</param>
/// <param name="IsPresent">Whether the student attended.</param>
/// <param name="Comment">An optional comment, for example the reason for absence.</param>
public sealed record AttendanceMark(Guid ContractId, bool IsPresent, string? Comment = null);
