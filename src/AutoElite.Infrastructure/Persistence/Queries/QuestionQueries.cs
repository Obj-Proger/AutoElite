using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Questions;
using AutoElite.Application.TestTemplates;
using AutoElite.Domain.Enums;
using AutoElite.Infrastructure.Persistence.Configurations;

namespace AutoElite.Infrastructure.Persistence.Queries;

/// <summary>Reads the question bank with the topics and courses of the questions, without tracking.</summary>
internal sealed class QuestionQueries(AutoEliteDbContext dbContext) : IQuestionQueries
{
    /// <inheritdoc />
    /// <remarks>
    /// The search looks in the question text and matches the external code whole, regardless of case: codes are
    /// identifiers, and their case-insensitive collation does not support pattern matching on every PostgreSQL version.
    /// </remarks>
    public async Task<PagedList<QuestionListItem>> GetListAsync(
        QuestionListFilter filter, PageRequest page, CancellationToken cancellationToken = default)
    {
        var rows = Rows();
        if (QueryExtensions.ContainsPattern(filter.Search) is { } pattern)
        {
            var code = filter.Search!.Trim();
            rows = rows.Where(row =>
                EF.Functions.ILike(row.Question.Text, pattern, QueryExtensions.LikeEscape)
                || row.Question.ExternalCode == code);
        }

        if (filter.CourseId is { } courseId)
            rows = rows.Where(row => row.CourseId == courseId);

        if (filter.TopicId is { } topicId)
            rows = rows.Where(row => row.Question.TopicId == topicId);

        if (filter.Category is { } category)
            rows = rows.Where(row => row.Question.Categories.Contains(category));

        if (filter.Source is { } source)
            rows = rows.Where(row => row.Question.Source == source);

        if (filter.IsActive is { } isActive)
            rows = rows.Where(row => row.Question.IsActive == isActive);

        var ordered = rows
            .OrderBy(row => row.CourseName)
            .ThenBy(row => row.TopicPosition)
            .ThenBy(row => row.Question.Text)
            .ThenBy(row => row.Question.Id);
        var (found, totalCount) = await ordered.ReadPageAsync(page, cancellationToken);
        QuestionListItem[] items =
        [
            .. found.Select(row => new QuestionListItem(
                row.Question.Id, row.Question.Text, row.Question.TopicId, row.TopicTitle, row.CourseName,
                row.Question.Categories, row.Question.Source, row.Question.ExternalCode,
                row.Question.ImageDocumentId is not null, row.Question.IsActive))
        ];

        return new PagedList<QuestionListItem>(items, page, totalCount);
    }

    /// <inheritdoc />
    public async Task<QuestionDetails?> GetDetailsAsync(Guid questionId, CancellationToken cancellationToken = default)
    {
        var row = await (
                from candidate in dbContext.Set<Question>().AsNoTracking()
                    .Include(withOptions => withOptions.Options.OrderBy(option => option.Position))
                join topic in dbContext.Set<CourseTopic>() on candidate.TopicId equals topic.Id
                join course in dbContext.Set<Course>()
                    on EF.Property<Guid>(topic, CourseConfiguration.CourseKey) equals course.Id
                where candidate.Id == questionId
                select new { Question = candidate, TopicTitle = topic.Title, CourseId = course.Id, CourseName = course.Name })
            .FirstOrDefaultAsync(cancellationToken);
        if (row is null)
            return null;

        var question = row.Question;

        return new QuestionDetails(
            question.Id, question.Text, question.Explanation, question.ImageDocumentId, question.TopicId, row.TopicTitle,
            row.CourseId, row.CourseName, question.Categories, question.Source, question.ExternalCode, question.IsActive,
            [
                .. question.Options.Select(option =>
                    new AnswerOptionItem(option.Id, option.Text, option.Position, option.IsCorrect))
            ]);
    }

    private IQueryable<QuestionRow> Rows() =>
        from question in dbContext.Set<Question>().AsNoTracking()
        join topic in dbContext.Set<CourseTopic>() on question.TopicId equals topic.Id
        join course in dbContext.Set<Course>() on EF.Property<Guid>(topic, CourseConfiguration.CourseKey) equals course.Id
        select new QuestionRow
        {
            Question = question,
            TopicTitle = topic.Title,
            TopicPosition = topic.Position,
            CourseId = course.Id,
            CourseName = course.Name
        };

    /// <summary>A question with the topic and course it belongs to.</summary>
    private sealed class QuestionRow
    {
        public Question Question { get; init; } = null!;

        public string TopicTitle { get; init; } = null!;

        public int TopicPosition { get; init; }

        public Guid CourseId { get; init; }

        public string CourseName { get; init; } = null!;
    }
}

/// <summary>Reads test templates with the titles of the topics of their blocks, without tracking.</summary>
internal sealed class TestTemplateQueries(AutoEliteDbContext dbContext) : ITestTemplateQueries
{
    /// <inheritdoc />
    /// <remarks>Categories are stored by name, so the few templates are sorted by category order in memory.</remarks>
    public async Task<IReadOnlyList<TestTemplateItem>> GetListAsync(
        LicenseCategory? category, bool? isActive, CancellationToken cancellationToken = default)
    {
        var templates = dbContext.Set<TestTemplate>().AsNoTracking();
        if (category is { } wanted)
            templates = templates.Where(template => template.Category == wanted);

        if (isActive is { } active)
            templates = templates.Where(template => template.IsActive == active);

        var found = await templates.OrderBy(template => template.Name).ToListAsync(cancellationToken);

        return
        [
            .. found.OrderBy(template => template.Category).Select(template => new TestTemplateItem(
                template.Id, template.Name, template.Category, template.Rules.QuestionCount,
                (int)template.Rules.TimeLimit.TotalMinutes, template.Rules.MaxMistakes, template.Source, template.IsActive))
        ];
    }

    /// <inheritdoc />
    public async Task<TestTemplateDetails?> GetDetailsAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await dbContext.Set<TestTemplate>().AsNoTracking()
            .FirstOrDefaultAsync(found => found.Id == templateId, cancellationToken);
        if (template is null)
            return null;

        List<Guid> topicIds = [.. template.Blocks.SelectMany(block => block.TopicIds).Distinct()];
        var titles = await dbContext.Set<CourseTopic>().AsNoTracking()
            .Where(topic => topicIds.Contains(topic.Id))
            .ToDictionaryAsync(topic => topic.Id, topic => topic.Title, cancellationToken);
        var rules = template.Rules;

        return new TestTemplateDetails(
            template.Id, template.Name, template.Category, rules.QuestionCount, (int)rules.TimeLimit.TotalMinutes,
            rules.MaxMistakes, rules.MaxMistakesPerBlock, rules.ExtraQuestionsPerMistake,
            (int)rules.ExtraTimePerMistake.TotalMinutes, template.Source, template.IsActive,
            [
                .. template.Blocks.Select(block => new TestBlockItem(
                    block.QuestionCount,
                    [
                        .. block.TopicIds.Select(topicId =>
                            new TestBlockTopicItem(topicId, titles.GetValueOrDefault(topicId, string.Empty)))
                    ]))
            ]);
    }
}
