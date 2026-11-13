namespace AutoElite.Client.Offline.Actions;

/// <summary>The attendance of one student at a theory lesson.</summary>
/// <param name="ContractId">The contract of the student.</param>
/// <param name="IsPresent">Whether the student attended.</param>
/// <param name="Comment">A comment, such as the reason for an absence, if any.</param>
public sealed record TheoryAttendanceMark(Guid ContractId, bool IsPresent, string? Comment = null);
