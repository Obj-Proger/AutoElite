namespace AutoElite.Application.TestSessions.AnswerTestQuestion;

/// <summary>
/// Records the answer of the signed-in student to a question of their attempt. A mistake that the rules allow adds
/// questions from the same block and extra time (FR-7.6); whether an answer is correct is shown only after the attempt
/// (FR-7.7). An attempt whose time has run out is closed instead.
/// </summary>
/// <param name="SessionId">The identifier of the attempt.</param>
/// <param name="SessionQuestionId">The question of the attempt.</param>
/// <param name="OptionId">The chosen answer option.</param>
[RequireRoles(UserRole.Student)]
public sealed record AnswerTestQuestionCommand(Guid SessionId, Guid SessionQuestionId, Guid OptionId) : ICommand<TestSessionStatus>;
