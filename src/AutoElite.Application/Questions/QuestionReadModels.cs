namespace AutoElite.Application.Questions;

/// <summary>The conditions of the question list; a condition that is <see langword="null"/> does not filter.</summary>
/// <param name="Search">Text to find in the question text or the external code, case-insensitively.</param>
/// <param name="CourseId">Only questions on the topics of this course.</param>
/// <param name="TopicId">Only questions on this topic.</param>
/// <param name="Category">Only questions that apply to this licence category.</param>
/// <param name="Source">Only official or only school questions.</param>
/// <param name="IsActive">Only active or only deactivated questions.</param>
public sealed record QuestionListFilter(
    string? Search,
    Guid? CourseId,
    Guid? TopicId,
    LicenseCategory? Category,
    QuestionSource? Source,
    bool? IsActive);

/// <summary>A row of the question list.</summary>
/// <param name="Id">The identifier of the question.</param>
/// <param name="Text">The question text.</param>
/// <param name="TopicId">The course topic.</param>
/// <param name="TopicTitle">The title of the topic.</param>
/// <param name="CourseName">The name of the course of the topic.</param>
/// <param name="Categories">The licence categories, in order.</param>
/// <param name="Source">Whether the question is official or written by the school.</param>
/// <param name="ExternalCode">The code in the official set, if any.</param>
/// <param name="HasImage">Whether the question has an image.</param>
/// <param name="IsActive">Whether the question is used in new tests.</param>
public sealed record QuestionListItem(
    Guid Id,
    string Text,
    Guid TopicId,
    string TopicTitle,
    string CourseName,
    IReadOnlyList<LicenseCategory> Categories,
    QuestionSource Source,
    string? ExternalCode,
    bool HasImage,
    bool IsActive);

/// <summary>An answer option of a question.</summary>
/// <param name="Id">The identifier of the option.</param>
/// <param name="Text">The text.</param>
/// <param name="Position">The position, from 1.</param>
/// <param name="IsCorrect">Whether this is the correct answer.</param>
public sealed record AnswerOptionItem(Guid Id, string Text, int Position, bool IsCorrect);

/// <summary>The card of a question with its answer options and the correct answer.</summary>
/// <param name="Id">The identifier of the question.</param>
/// <param name="Text">The question text.</param>
/// <param name="Explanation">The explanation, if any.</param>
/// <param name="ImageDocumentId">The document with the image, if any; anyone signed in can download it.</param>
/// <param name="TopicId">The course topic.</param>
/// <param name="TopicTitle">The title of the topic.</param>
/// <param name="CourseId">The course of the topic.</param>
/// <param name="CourseName">The name of the course.</param>
/// <param name="Categories">The licence categories, in order.</param>
/// <param name="Source">Whether the question is official or written by the school.</param>
/// <param name="ExternalCode">The code in the official set, if any.</param>
/// <param name="IsActive">Whether the question is used in new tests.</param>
/// <param name="Options">The answer options by position.</param>
public sealed record QuestionDetails(
    Guid Id,
    string Text,
    string? Explanation,
    Guid? ImageDocumentId,
    Guid TopicId,
    string TopicTitle,
    Guid CourseId,
    string CourseName,
    IReadOnlyList<LicenseCategory> Categories,
    QuestionSource Source,
    string? ExternalCode,
    bool IsActive,
    IReadOnlyList<AnswerOptionItem> Options);
