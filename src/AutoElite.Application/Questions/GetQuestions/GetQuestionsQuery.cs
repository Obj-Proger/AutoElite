using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.Questions.GetQuestions;

/// <summary>Reads a page of the question bank, the latest added first.</summary>
/// <param name="Search">Text to find in the question text or the external code.</param>
/// <param name="CourseId">Only questions on the topics of this course.</param>
/// <param name="TopicId">Only questions on this topic.</param>
/// <param name="Category">Only questions that apply to this licence category.</param>
/// <param name="Source">Only official or only school questions.</param>
/// <param name="IsActive">Only active or only deactivated questions.</param>
/// <param name="Page">The page number, starting from 1.</param>
/// <param name="PageSize">The number of questions per page.</param>
[RequireRoles(UserRole.Administrator, UserRole.TheoryInstructor)]
public sealed record GetQuestionsQuery(
    string? Search = null,
    Guid? CourseId = null,
    Guid? TopicId = null,
    LicenseCategory? Category = null,
    QuestionSource? Source = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = PageRequest.DefaultPageSize) : IQuery<PagedList<QuestionListItem>>, IPagedQuery;
