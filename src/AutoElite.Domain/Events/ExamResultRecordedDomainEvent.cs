using AutoElite.Domain.Enums;

namespace AutoElite.Domain.Events;

/// <summary>Raised when an exam result is recorded or corrected; the student is notified.</summary>
/// <param name="ExamId">The identifier of the exam.</param>
/// <param name="ContractId">The identifier of the student's contract.</param>
/// <param name="StudentId">The identifier of the student.</param>
/// <param name="Kind">Theory or practice.</param>
/// <param name="Passed">Whether the student passed.</param>
/// <param name="OccurredOnUtc">The UTC time the result was recorded or corrected.</param>
public sealed record ExamResultRecordedDomainEvent(
    Guid ExamId, Guid ContractId, Guid StudentId, ExamKind Kind, bool Passed, DateTime OccurredOnUtc) : IDomainEvent;
