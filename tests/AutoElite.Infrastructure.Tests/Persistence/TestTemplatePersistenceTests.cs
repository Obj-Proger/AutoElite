using AutoElite.Application.TestTemplates;
using AutoElite.Domain.Enums;
using AutoElite.Infrastructure.Persistence.Queries;
using AutoElite.Infrastructure.Persistence.Repositories;

namespace AutoElite.Infrastructure.Tests.Persistence;

public sealed class TestTemplatePersistenceTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task SaveAsync(params object[] records)
    {
        await using var context = database.CreateContext();
        context.AddRange(records);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
    }

    private static TestBlock Block(int questionCount, params Guid[] topicIds) => TestBlock.Create(questionCount, topicIds).Value;

    private static TestTemplate NewTemplate(string name, params TestBlock[] blocks) =>
        TestTemplate.Create(name, LicenseCategory.B, TestRules.TrafficPoliceExam, blocks, QuestionSource.Official).Value;

    [Fact]
    public async Task Save_Template_StoresItsRulesAndBlocks()
    {
        var course = TrainingRecords.NewCourse();
        var signs = course.AddTopic("Знаки", null).Value.Id;
        var markings = course.AddTopic("Разметка", null).Value.Id;
        var template = NewTemplate("Экзамен ГИБДД", Block(15, signs), Block(5, markings));
        await SaveAsync(course, template);

        await using (var context = database.CreateContext())
        {
            var tracked = await new TestTemplateRepository(context).GetByIdAsync(template.Id, CancellationToken);
            tracked!.Rules.Should().Be(TestRules.TrafficPoliceExam);
            tracked.Blocks.Should().Equal(Block(15, signs), Block(5, markings));
            tracked.UpdateDetails(
                    "Экзамен ГИБДД", LicenseCategory.B, TestRules.TrafficPoliceExam, [Block(20, signs, markings)], null)
                .IsSuccess.Should().BeTrue();
            await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
        }

        await using var reader = database.CreateContext();
        var loaded = await new TestTemplateRepository(reader).GetByIdAsync(template.Id, CancellationToken);
        loaded!.Blocks.Should().Equal(Block(20, signs, markings));
        loaded.Source.Should().BeNull();
        (await new CourseRepository(reader).IsTopicInUseAsync(markings, CancellationToken)).Should().BeTrue();
    }

    [Fact]
    public async Task IsTopicInUse_TopicNobodyUses_ReturnsFalse()
    {
        var course = TrainingRecords.NewCourse();
        var used = course.AddTopic("Знаки", null).Value.Id;
        var unused = course.AddTopic("Медицина", null).Value.Id;
        await SaveAsync(course, NewTemplate("Пробный тест", Block(20, used)));
        await using var context = database.CreateContext();

        (await new CourseRepository(context).IsTopicInUseAsync(unused, CancellationToken)).Should().BeFalse();
    }

    [Fact]
    public async Task Queries_ReadTheTemplateWithTheTitlesOfItsTopics()
    {
        var course = TrainingRecords.NewCourse();
        var signs = course.AddTopic("Знаки", null).Value.Id;
        var template = NewTemplate($"Тест {Guid.NewGuid():N}", Block(20, signs));
        await SaveAsync(course, template);
        await using var context = database.CreateContext();
        var queries = new TestTemplateQueries(context);

        var list = await queries.GetListAsync(LicenseCategory.B, true, CancellationToken);
        var details = await queries.GetDetailsAsync(template.Id, CancellationToken);

        list.Should().Contain(item => item.Id == template.Id && item.QuestionCount == 20 && item.TimeLimitMinutes == 20);
        details!.MaxMistakesPerBlock.Should().Be(1);
        details.ExtraMinutesPerMistake.Should().Be(5);
        details.Blocks.Should().ContainSingle().Which.Topics.Should().Equal(new TestBlockTopicItem(signs, "Знаки"));
        (await queries.GetDetailsAsync(Guid.CreateVersion7(), CancellationToken)).Should().BeNull();
    }
}
