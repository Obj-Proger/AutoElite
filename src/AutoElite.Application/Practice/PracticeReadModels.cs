namespace AutoElite.Application.Practice;

/// <summary>How far a student has gone through the questions of the licence category of a contract.</summary>
/// <param name="QuestionCount">The active questions of the category.</param>
/// <param name="AnsweredCount">How many of them the student has answered in practice.</param>
/// <param name="MistakeCount">How many of them the student last answered wrong.</param>
/// <param name="Topics">The topics of the questions, in the order of their courses.</param>
public sealed record PracticeOverview(int QuestionCount, int AnsweredCount, int MistakeCount, IReadOnlyList<PracticeTopic> Topics);

/// <summary>How far a student has gone through the questions of one topic.</summary>
/// <param name="TopicId">The course topic.</param>
/// <param name="Title">The title of the topic.</param>
/// <param name="QuestionCount">The active questions of the topic and the category.</param>
/// <param name="AnsweredCount">How many of them the student has answered in practice.</param>
/// <param name="MistakeCount">How many of them the student last answered wrong.</param>
public sealed record PracticeTopic(Guid TopicId, string Title, int QuestionCount, int AnsweredCount, int MistakeCount);

/// <summary>A part of the questions to practise, in the order of the practice.</summary>
/// <param name="Total">How many questions the practice has in all.</param>
/// <param name="Questions">The questions of the part.</param>
public sealed record PracticeQuestionPage(int Total, IReadOnlyList<PracticeQuestion> Questions);

/// <summary>A question to practise, without its correct option, which the answer reveals.</summary>
/// <param name="Id">The question of the bank.</param>
/// <param name="TopicId">The course topic of the question.</param>
/// <param name="Text">The text of the question.</param>
/// <param name="ImageDocumentId">The document with the illustration, if the question has one.</param>
/// <param name="Options">The answer options, in their order.</param>
public sealed record PracticeQuestion(
    Guid Id, Guid TopicId, string Text, Guid? ImageDocumentId, IReadOnlyList<PracticeOption> Options);

/// <summary>An answer option of a question to practise.</summary>
/// <param name="Id">The option.</param>
/// <param name="Text">The text of the option.</param>
public sealed record PracticeOption(Guid Id, string Text);

/// <summary>Whether an answer in practice was right, with the correct option and the explanation.</summary>
/// <param name="IsCorrect">Whether the chosen option is the correct one.</param>
/// <param name="CorrectOptionId">The correct option.</param>
/// <param name="Explanation">The explanation of the correct answer, if the question has one.</param>
public sealed record PracticeAnswerResult(bool IsCorrect, Guid CorrectOptionId, string? Explanation);
