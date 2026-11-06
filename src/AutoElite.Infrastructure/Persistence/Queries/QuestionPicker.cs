using AutoElite.Application.TestSessions;

namespace AutoElite.Infrastructure.Persistence.Queries;

/// <summary>
/// Draws random questions from the bank with PostgreSQL's <c>random()</c>, so that only the questions drawn are read.
/// The identifiers are drawn first and the questions with their options read after: a random order is evaluated anew by
/// every query and on every row, so it must not reach the query that joins the options. The questions are not tracked;
/// a test only copies them.
/// </summary>
internal sealed class QuestionPicker(AutoEliteDbContext dbContext) : IQuestionPicker
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Question>> PickAsync(QuestionPick pick, CancellationToken cancellationToken = default)
    {
        if (pick.Count <= 0)
            return [];

        var category = pick.Category;
        List<Guid> topicIds = [.. pick.TopicIds];
        List<Guid> excluded = [.. pick.ExcludedQuestionIds];
        var questions = dbContext.Set<Question>()
            .AsNoTracking()
            .Where(question => question.IsActive && question.Categories.Contains(category));
        if (pick.Source is { } source)
            questions = questions.Where(question => question.Source == source);

        if (topicIds.Count > 0)
            questions = questions.Where(question => topicIds.Contains(question.TopicId));

        if (excluded.Count > 0)
            questions = questions.Where(question => !excluded.Contains(question.Id));

        var drawnIds = await questions
            .OrderBy(_ => EF.Functions.Random())
            .Take(pick.Count)
            .Select(question => question.Id)
            .ToListAsync(cancellationToken);
        if (drawnIds.Count == 0)
            return [];

        var drawn = await dbContext.Set<Question>()
            .AsNoTracking()
            .Where(question => drawnIds.Contains(question.Id))
            .Include(question => question.Options.OrderBy(option => option.Position))
            .ToDictionaryAsync(question => question.Id, cancellationToken);

        return [.. drawnIds.Select(id => drawn[id])];
    }
}
