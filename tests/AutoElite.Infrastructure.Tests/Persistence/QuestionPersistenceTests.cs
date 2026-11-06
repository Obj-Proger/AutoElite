using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Questions;
using AutoElite.Domain.Enums;
using AutoElite.Infrastructure.Persistence.Queries;
using AutoElite.Infrastructure.Persistence.Repositories;

namespace AutoElite.Infrastructure.Tests.Persistence;

public sealed class QuestionPersistenceTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static readonly PageRequest _firstPage = new(1, PageRequest.MaxPageSize);

    private static readonly AnswerOptionDraft[] _options =
    [
        new("Остановиться", true),
        new("Продолжить движение", false),
        new("Подать сигнал", false)
    ];

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task SaveAsync(params object[] records)
    {
        await using var context = database.CreateContext();
        context.AddRange(records);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
    }

    /// <summary>A course with two topics, saved.</summary>
    private async Task<(Course Course, Guid FirstTopic, Guid SecondTopic)> CourseAsync()
    {
        var course = TrainingRecords.NewCourse();
        var first = course.AddTopic("Дорожные знаки", null).Value.Id;
        var second = course.AddTopic("Разметка", null).Value.Id;
        await SaveAsync(course);

        return (course, first, second);
    }

    private static Question NewQuestion(
        Guid topicId, string text = "Что означает знак?", string? code = null, LicenseCategory[]? categories = null) =>
        Question.Create(text, "Пояснение", topicId, categories ?? [LicenseCategory.B, LicenseCategory.A], QuestionSource.Official,
                code, _options)
            .Value;

    private static string Code() => "Q-" + Guid.NewGuid().ToString("N")[..12];

    [Fact]
    public async Task Save_Question_StoresItsOptionsInOrderAndItsCategories()
    {
        var (_, topicId, _) = await CourseAsync();
        var question = NewQuestion(topicId, code: Code());
        await SaveAsync(question);

        await using var context = database.CreateContext();
        var loaded = await new QuestionRepository(context).GetByIdAsync(question.Id, CancellationToken);

        loaded!.Options.Select(option => option.Text).Should().Equal("Остановиться", "Продолжить движение", "Подать сигнал");
        loaded.CorrectOption.Text.Should().Be("Остановиться");
        loaded.Categories.Should().Equal(LicenseCategory.A, LicenseCategory.B);
        loaded.ExternalCode.Should().Be(question.ExternalCode);
        (await new CourseRepository(context).IsTopicInUseAsync(topicId, CancellationToken)).Should().BeTrue();
    }

    [Fact]
    public async Task Save_NewContent_ReplacesTheOptions()
    {
        var (_, topicId, otherTopicId) = await CourseAsync();
        var question = NewQuestion(topicId);
        await SaveAsync(question);

        await using (var context = database.CreateContext())
        {
            var tracked = await new QuestionRepository(context).GetByIdAsync(question.Id, CancellationToken);
            tracked!.UpdateContent(
                    "Новый текст", null, otherTopicId, [LicenseCategory.C], null, [new("Да", false), new("Нет", true)])
                .IsSuccess.Should().BeTrue();
            await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
        }

        await using var reader = database.CreateContext();
        var loaded = await new QuestionRepository(reader).GetByIdAsync(question.Id, CancellationToken);
        loaded!.Text.Should().Be("Новый текст");
        loaded.TopicId.Should().Be(otherTopicId);
        loaded.Categories.Should().Equal(LicenseCategory.C);
        loaded.Options.Select(option => (option.Text, option.IsCorrect)).Should().Equal(("Да", false), ("Нет", true));
    }

    [Fact]
    public async Task ExternalCodes_AreUniqueAndMatchedRegardlessOfCase()
    {
        var (_, topicId, _) = await CourseAsync();
        var code = Code();
        var question = NewQuestion(topicId, code: code);
        await SaveAsync(question);
        await using (var context = database.CreateContext())
        {
            var questions = new QuestionRepository(context);
            (await questions.ExternalCodeExistsAsync(code.ToLowerInvariant(), null, CancellationToken)).Should().BeTrue();
            (await questions.ExternalCodeExistsAsync(code, question.Id, CancellationToken)).Should().BeFalse();
            (await questions.GetByExternalCodesAsync([code.ToLowerInvariant(), "no-such-code"], CancellationToken))
                .Should().ContainSingle().Which.Options.Should().HaveCount(3);
        }

        var save = () => SaveAsync(NewQuestion(topicId, code: code.ToLowerInvariant()));

        await save.Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task Queries_FilterTheBankAndReadTheCard()
    {
        var (course, firstTopic, secondTopic) = await CourseAsync();
        var marker = Guid.NewGuid().ToString("N")[..10];
        var code = Code();
        var ofFirstTopic = NewQuestion(firstTopic, $"Знак {marker}", code);
        var ofSecondTopic = NewQuestion(secondTopic, $"Разметка {marker}", categories: [LicenseCategory.C]);
        await SaveAsync(ofFirstTopic, ofSecondTopic);
        await using var context = database.CreateContext();
        var queries = new QuestionQueries(context);

        async Task<IEnumerable<Guid>> IdsAsync(QuestionListFilter filter) =>
            (await queries.GetListAsync(filter, _firstPage, CancellationToken)).Items.Select(item => item.Id);

        (await IdsAsync(Filter(marker))).Should().Equal(ofFirstTopic.Id, ofSecondTopic.Id);
        (await IdsAsync(Filter(code.ToLowerInvariant()))).Should().Equal(ofFirstTopic.Id);
        (await IdsAsync(Filter(marker) with { Category = LicenseCategory.C })).Should().Equal(ofSecondTopic.Id);
        (await IdsAsync(Filter(null) with { CourseId = course.Id, TopicId = secondTopic })).Should().Equal(ofSecondTopic.Id);

        var details = await queries.GetDetailsAsync(ofFirstTopic.Id, CancellationToken);
        details!.TopicTitle.Should().Be("Дорожные знаки");
        details.CourseId.Should().Be(course.Id);
        details.Options.Select(option => option.Position).Should().Equal(1, 2, 3);
        (await queries.GetDetailsAsync(Guid.CreateVersion7(), CancellationToken)).Should().BeNull();
    }

    private static QuestionListFilter Filter(string? search) => new(search, null, null, null, null, null);
}
