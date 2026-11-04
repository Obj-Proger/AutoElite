using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.Questions.GetQuestions;

/// <summary>Reads the page; the bank is shared by the whole school.</summary>
internal sealed class GetQuestionsQueryHandler(IQuestionQueries questionQueries)
    : IQueryHandler<GetQuestionsQuery, PagedList<QuestionListItem>>
{
    /// <inheritdoc />
    public async Task<Result<PagedList<QuestionListItem>>> Handle(GetQuestionsQuery query, CancellationToken cancellationToken)
    {
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var filter = new QuestionListFilter(search, query.CourseId, query.TopicId, query.Category, query.Source, query.IsActive);

        return await questionQueries.GetListAsync(filter, PageRequest.From(query), cancellationToken);
    }
}
