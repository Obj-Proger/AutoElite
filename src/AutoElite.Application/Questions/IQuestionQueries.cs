using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.Questions;

/// <summary>Reads the question bank for queries.</summary>
public interface IQuestionQueries
{
    /// <summary>Reads one page of the questions that match the filter, the latest added first.</summary>
    /// <param name="filter">The conditions of the list.</param>
    /// <param name="page">The page to read.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<PagedList<QuestionListItem>> GetListAsync(
        QuestionListFilter filter, PageRequest page, CancellationToken cancellationToken = default);

    /// <summary>Reads the card of a question.</summary>
    /// <param name="questionId">The identifier of the question.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The card, or <see langword="null"/> if the question does not exist.</returns>
    Task<QuestionDetails?> GetDetailsAsync(Guid questionId, CancellationToken cancellationToken = default);
}
