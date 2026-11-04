namespace AutoElite.Application.Questions.DeactivateQuestion;

/// <summary>Takes a question out of new tests; tests already taken keep it. Only administrators change official questions.</summary>
/// <param name="QuestionId">The identifier of the question.</param>
[RequireRoles(UserRole.Administrator, UserRole.TheoryInstructor)]
public sealed record DeactivateQuestionCommand(Guid QuestionId) : ICommand;
