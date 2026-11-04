using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Questions;

namespace AutoElite.Application.Tests.TestDoubles;

/// <summary>Returns seeded cards and records the list arguments; filtering is the infrastructure's job.</summary>
internal sealed class FakeQuestionQueries : IQuestionQueries
{
    private readonly Dictionary<Guid, QuestionDetails> _details = [];

    public (QuestionListFilter Filter, PageRequest Page)? LastListCall { get; private set; }

    public void Seed(QuestionDetails details) => _details.Add(details.Id, details);

    public Task<PagedList<QuestionListItem>> GetListAsync(
        QuestionListFilter filter, PageRequest page, CancellationToken cancellationToken = default)
    {
        LastListCall = (filter, page);

        return Task.FromResult(PagedList<QuestionListItem>.Empty(page));
    }

    public Task<QuestionDetails?> GetDetailsAsync(Guid questionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_details.GetValueOrDefault(questionId));
}
