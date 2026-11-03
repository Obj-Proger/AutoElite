namespace AutoElite.Domain.Events;

/// <summary>Raised when a theory lesson is moved; the members of the group are notified and reminders are rescheduled.</summary>
/// <param name="LessonId">The identifier of the lesson.</param>
/// <param name="GroupId">The identifier of the study group.</param>
/// <param name="StartUtc">The new UTC start.</param>
/// <param name="EndUtc">The new UTC end.</param>
/// <param name="OccurredOnUtc">The UTC time of the change.</param>
public sealed record TheoryLessonRescheduledDomainEvent(Guid LessonId, Guid GroupId, DateTime StartUtc, DateTime EndUtc, DateTime OccurredOnUtc) : IDomainEvent;
