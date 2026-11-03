namespace AutoElite.Domain.Events;

/// <summary>Raised when a theory lesson is cancelled; the members of the group are notified.</summary>
/// <param name="LessonId">The identifier of the lesson.</param>
/// <param name="GroupId">The identifier of the study group.</param>
/// <param name="StartUtc">The UTC start of the cancelled lesson.</param>
/// <param name="Reason">Why the lesson was cancelled.</param>
/// <param name="OccurredOnUtc">The UTC time of the cancellation.</param>
public sealed record TheoryLessonCancelledDomainEvent(Guid LessonId, Guid GroupId, DateTime StartUtc, string Reason, DateTime OccurredOnUtc) : IDomainEvent;
