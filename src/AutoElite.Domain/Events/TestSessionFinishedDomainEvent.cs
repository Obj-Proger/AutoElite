namespace AutoElite.Domain.Events;

/// <summary>Raised when a test attempt passes or fails; the result of a theory exam is recorded from it (FR-8.3).</summary>
/// <param name="SessionId">The identifier of the attempt.</param>
/// <param name="ContractId">The identifier of the student's contract.</param>
/// <param name="StudentId">The identifier of the student.</param>
/// <param name="ExamId">The exam the attempt belongs to, or <see langword="null"/> for practice.</param>
/// <param name="Passed">Whether the attempt passed.</param>
/// <param name="CorrectAnswers">The number of correct answers.</param>
/// <param name="Mistakes">The number of wrong answers in main questions.</param>
/// <param name="OccurredOnUtc">The UTC time the attempt finished.</param>
public sealed record TestSessionFinishedDomainEvent(
    Guid SessionId,
    Guid ContractId,
    Guid StudentId,
    Guid? ExamId,
    bool Passed,
    int CorrectAnswers,
    int Mistakes,
    DateTime OccurredOnUtc) : IDomainEvent;
