using AutoElite.Domain.Enums;

namespace AutoElite.Domain.Events;

/// <summary>Raised when an exam is scheduled; the members of the group and their theory instructor are notified (FR-8.6).</summary>
/// <param name="ExamId">The identifier of the exam.</param>
/// <param name="GroupId">The identifier of the study group.</param>
/// <param name="Kind">Theory or practice.</param>
/// <param name="ScheduledAtUtc">When the exam starts.</param>
/// <param name="OccurredOnUtc">The UTC time the exam was scheduled.</param>
public sealed record ExamScheduledDomainEvent(Guid ExamId, Guid GroupId, ExamKind Kind, DateTime ScheduledAtUtc, DateTime OccurredOnUtc) : IDomainEvent;
