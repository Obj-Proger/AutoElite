namespace AutoElite.Domain.Events;

/// <summary>Raised when an exam is moved; the members of the group and their theory instructor are notified.</summary>
/// <param name="ExamId">The identifier of the exam.</param>
/// <param name="GroupId">The identifier of the study group.</param>
/// <param name="ScheduledAtUtc">The new start.</param>
/// <param name="OccurredOnUtc">The UTC time of the change.</param>
public sealed record ExamRescheduledDomainEvent(Guid ExamId, Guid GroupId, DateTime ScheduledAtUtc, DateTime OccurredOnUtc) : IDomainEvent;
