namespace AutoElite.Application.TestSessions.GetTestSession;

/// <summary>
/// Reads the card of a test attempt with its questions. While the attempt is in progress it shows the questions and
/// the chosen answers only; after it, the correct answers and the explanations too (FR-7.7). The student reads their
/// own attempts; managers read the attempts of contracts in their scope.
/// </summary>
/// <param name="SessionId">The identifier of the attempt.</param>
[RequireRoles(UserRole.Student, UserRole.Manager, UserRole.Administrator)]
public sealed record GetTestSessionQuery(Guid SessionId) : IQuery<TestSessionDetails>;
