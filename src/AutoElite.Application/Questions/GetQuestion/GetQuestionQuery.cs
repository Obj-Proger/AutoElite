namespace AutoElite.Application.Questions.GetQuestion;

/// <summary>Reads the card of a question with its answer options and the correct answer.</summary>
/// <param name="QuestionId">The identifier of the question.</param>
[RequireRoles(UserRole.Administrator, UserRole.TheoryInstructor)]
public sealed record GetQuestionQuery(Guid QuestionId) : IQuery<QuestionDetails>;
