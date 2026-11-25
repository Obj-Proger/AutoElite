namespace AutoElite.Application.Practice.GetPracticeQuestions;

/// <summary>
/// Reads a part of the questions the signed-in student practises on a contract: those of a topic, all of the licence
/// category as a marathon, or the student's mistakes. The questions are shuffled by a number the client chooses once
/// for the practice, so that the parts read with the same number follow one order; the correct options are left out.
/// </summary>
/// <param name="ContractId">A contract of the student, whose licence category chooses the questions.</param>
/// <param name="Mode">Which questions are practised.</param>
/// <param name="TopicId">The topic, for the practice of a topic.</param>
/// <param name="Seed">The number that shuffles the questions, the same for every part of a practice.</param>
/// <param name="Offset">How many questions of the practice come before the part.</param>
/// <param name="Count">How many questions the part has, from 1 to <see cref="GetPracticeQuestionsQueryValidator.MaxCount"/>.</param>
[RequireRoles(UserRole.Student)]
public sealed record GetPracticeQuestionsQuery(
    Guid ContractId, PracticeMode Mode, Guid? TopicId, int Seed, int Offset, int Count) : IQuery<PracticeQuestionPage>;
