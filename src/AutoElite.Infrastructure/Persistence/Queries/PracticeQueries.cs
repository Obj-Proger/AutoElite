using AutoElite.Application.Practice;
using AutoElite.Domain.Enums;

namespace AutoElite.Infrastructure.Persistence.Queries;

/// <summary>
/// Reads the questions that students practise and their practice records, without tracking. The counts are grouped by
/// topic in the database; the questions are read with their options and given without the correct one.
/// </summary>
internal sealed class PracticeQueries(AutoEliteDbContext dbContext) : IPracticeQueries
{
    /// <inheritdoc />
    public async Task<PracticeOverview> GetOverviewAsync(
        Guid studentId, LicenseCategory category, CancellationToken cancellationToken = default)
    {
        var questions = ActiveQuestions(category);
        var questionCounts = await questions
            .GroupBy(question => question.TopicId)
            .Select(topic => new { TopicId = topic.Key, Count = topic.Count() })
            .ToListAsync(cancellationToken);
        var practised = await dbContext.Set<PracticeRecord>().AsNoTracking()
            .Where(record => record.StudentId == studentId)
            .Join(questions, record => record.QuestionId, question => question.Id, (record, question) => new
            {
                question.TopicId,
                record.IsLastAnswerCorrect
            })
            .GroupBy(answer => answer.TopicId)
            .Select(topic => new
            {
                TopicId = topic.Key,
                Answered = topic.Count(),
                Mistakes = topic.Count(answer => !answer.IsLastAnswerCorrect)
            })
            .ToDictionaryAsync(topic => topic.TopicId, cancellationToken);
        List<Guid> topicIds = [.. questionCounts.Select(topic => topic.TopicId)];
        var titles = await dbContext.Set<CourseTopic>().AsNoTracking()
            .Where(topic => topicIds.Contains(topic.Id))
            .Select(topic => new { topic.Id, topic.Title, topic.Position })
            .ToDictionaryAsync(topic => topic.Id, cancellationToken);
        List<PracticeTopic> topics =
        [
            .. questionCounts
                .Where(topic => titles.ContainsKey(topic.TopicId))
                .OrderBy(topic => titles[topic.TopicId].Position)
                .ThenBy(topic => titles[topic.TopicId].Title, StringComparer.CurrentCulture)
                .Select(topic => new PracticeTopic(
                    topic.TopicId,
                    titles[topic.TopicId].Title,
                    topic.Count,
                    practised.TryGetValue(topic.TopicId, out var answers) ? answers.Answered : 0,
                    answers?.Mistakes ?? 0))
        ];

        return new PracticeOverview(
            topics.Sum(topic => topic.QuestionCount),
            topics.Sum(topic => topic.AnsweredCount),
            topics.Sum(topic => topic.MistakeCount),
            topics);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetQuestionIdsAsync(
        Guid studentId, LicenseCategory category, Guid? topicId, bool mistakesOnly, CancellationToken cancellationToken = default)
    {
        var questions = ActiveQuestions(category);
        if (topicId is { } topic)
            questions = questions.Where(question => question.TopicId == topic);

        if (mistakesOnly)
        {
            questions = questions.Where(question => dbContext.Set<PracticeRecord>().Any(record =>
                record.StudentId == studentId && record.QuestionId == question.Id && !record.IsLastAnswerCorrect));
        }

        return await questions.OrderBy(question => question.Id).Select(question => question.Id).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PracticeQuestion>> GetQuestionsAsync(
        IReadOnlyList<Guid> questionIds, CancellationToken cancellationToken = default)
    {
        List<Guid> ids = [.. questionIds];
        var found = await dbContext.Set<Question>().AsNoTracking()
            .Where(question => ids.Contains(question.Id))
            .Include(question => question.Options.OrderBy(option => option.Position))
            .ToDictionaryAsync(question => question.Id, cancellationToken);

        return
        [
            .. ids.Where(found.ContainsKey).Select(id => found[id]).Select(question => new PracticeQuestion(
                question.Id,
                question.TopicId,
                question.Text,
                question.ImageDocumentId,
                [.. question.Options.Select(option => new PracticeOption(option.Id, option.Text))]))
        ];
    }

    // The questions in use for a licence category; the practice never shows a question taken out of use.
    private IQueryable<Question> ActiveQuestions(LicenseCategory category) =>
        dbContext.Set<Question>().AsNoTracking().Where(question => question.IsActive && question.Categories.Contains(category));
}
