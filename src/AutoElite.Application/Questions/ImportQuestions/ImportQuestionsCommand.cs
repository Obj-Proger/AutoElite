namespace AutoElite.Application.Questions.ImportQuestions;

/// <summary>
/// Imports questions from a file into one topic (FR-7.1). A question whose external code is already in the bank is
/// updated, so that a new edition of the official set can be imported over the old one; other questions are added.
/// The import is all or nothing: if any row is invalid, nothing is imported and the errors are listed by row.
/// Illustrations the file brings, for example in a ZIP archive with the questions, become documents of their
/// questions; a row without one keeps the illustration the question has.
/// </summary>
/// <param name="Content">The content of the file, read once; the sender disposes it.</param>
/// <param name="FileName">The name of the file; its extension and content tell the format.</param>
/// <param name="TopicId">The course topic the questions go to.</param>
/// <param name="Source">Whether the questions are official, which only administrators import, or written by the school.</param>
[RequireRoles(UserRole.Administrator, UserRole.TheoryInstructor)]
public sealed record ImportQuestionsCommand(Stream? Content, string? FileName, Guid TopicId, QuestionSource Source)
    : ICommand<QuestionImportResult>;
