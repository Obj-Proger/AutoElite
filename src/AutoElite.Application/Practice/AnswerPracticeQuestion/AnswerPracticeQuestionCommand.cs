namespace AutoElite.Application.Practice.AnswerPracticeQuestion;

/// <summary>
/// Records the answer of the signed-in student to a question in practice and tells at once whether it was right, with
/// the correct option and the explanation. A wrong answer makes the question one of the student's mistakes; a right one
/// takes it off them.
/// </summary>
/// <param name="ContractId">A contract of the student, whose licence category the question must be of.</param>
/// <param name="QuestionId">An active question of the bank.</param>
/// <param name="OptionId">The chosen option of the question.</param>
[RequireRoles(UserRole.Student)]
public sealed record AnswerPracticeQuestionCommand(Guid ContractId, Guid QuestionId, Guid OptionId) : ICommand<PracticeAnswerResult>;
