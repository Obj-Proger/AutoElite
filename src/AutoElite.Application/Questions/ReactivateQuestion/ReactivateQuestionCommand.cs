namespace AutoElite.Application.Questions.ReactivateQuestion;

/// <summary>Returns a question to new tests. Only administrators change official questions.</summary>
/// <param name="QuestionId">The identifier of the question.</param>
[RequireRoles(UserRole.Administrator, UserRole.TheoryInstructor)]
public sealed record ReactivateQuestionCommand(Guid QuestionId) : ICommand;
