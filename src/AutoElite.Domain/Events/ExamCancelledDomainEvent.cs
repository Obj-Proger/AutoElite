namespace AutoElite.Domain.Events;

/// <summary>Raised when an exam is cancelled; the members of the group and their theory instructor are notified.</summary>
/// <param name="ExamId">The identifier of the exam.</param>
/// <param name="GroupId">The identifier of the study group.</param>
/// <param name="Reason">Why the exam was cancelled.</param>
/// <param name="OccurredOnUtc">The UTC time of the cancellation.</param>
public sealed record ExamCancelledDomainEvent(Guid ExamId, Guid GroupId, string Reason, DateTime OccurredOnUtc) : IDomainEvent;
