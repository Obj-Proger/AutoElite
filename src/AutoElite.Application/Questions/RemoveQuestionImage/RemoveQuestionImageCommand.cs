namespace AutoElite.Application.Questions.RemoveQuestionImage;

/// <summary>Removes the illustration of a question; the document stays among the question's documents.</summary>
/// <param name="QuestionId">The question.</param>
[RequireRoles(UserRole.Administrator, UserRole.TheoryInstructor)]
public sealed record RemoveQuestionImageCommand(Guid QuestionId) : ICommand;
