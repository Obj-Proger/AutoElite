using AutoElite.Application.TestSessions;
using AutoElite.Domain.Entities;

namespace AutoElite.Application.Tests.TestDoubles;

/// <summary>Draws matching questions in the order they were seeded, so that tests know which questions come.</summary>
internal sealed class FakeQuestionPicker : IQuestionPicker
{
    public List<Question> Bank { get; } = [];

    public Task<IReadOnlyList<Question>> PickAsync(QuestionPick pick, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Question>>([
            .. Bank.Where(question =>
                    question.IsActive
                    && question.Categories.Contains(pick.Category)
                    && (pick.Source is null || question.Source == pick.Source)
                    && (pick.TopicIds.Count == 0 || pick.TopicIds.Contains(question.TopicId))
                    && !pick.ExcludedQuestionIds.Contains(question.Id))
                .Take(pick.Count)
        ]);
}
