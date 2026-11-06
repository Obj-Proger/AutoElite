using AutoElite.Application.TestSessions;
using AutoElite.Domain.Enums;
using AutoElite.Infrastructure.Persistence.Queries;
using AutoElite.Infrastructure.Persistence.Repositories;

namespace AutoElite.Infrastructure.Tests.Persistence;

public sealed class TestSessionPersistenceTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static readonly AnswerOptionDraft[] _options =
    [
        new("Остановиться", true),
        new("Продолжить движение", false),
        new("Подать сигнал", false)
    ];

    /// <summary>Three questions, up to one mistake, each adding one question from its block and five minutes.</summary>
    private static readonly TestRules _rules =
        TestRules.Create(3, TimeSpan.FromMinutes(10), 1, 1, 1, TimeSpan.FromMinutes(5)).Value;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task SaveAsync(params object[] records)
    {
        await using var context = database.CreateContext();
        context.AddRange(records);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
    }

    private static Question NewQuestion(Guid topicId, string text, params LicenseCategory[] categories) =>
        Question.Create(
                text, "Пояснение", topicId, categories.Length > 0 ? categories : [LicenseCategory.B], QuestionSource.Official,
                null, _options)
            .Value;

    /// <summary>A contract, a topic of its own with the given number of questions, and a one-block template, saved.</summary>
    private async Task<(Contract Contract, TestTemplate Template, Guid TopicId, Question[] Questions)> SceneAsync(int questionCount)
    {
        var student = TrainingRecords.Person("Кузнецов");
        var course = TrainingRecords.NewCourse();
        var topicId = course.AddTopic("Дорожные знаки", null).Value.Id;
        var contract = TrainingRecords.NewContract(TrainingRecords.Number(), student, course);
        var template = TestTemplate.Create(
                $"Тест {Guid.NewGuid():N}", LicenseCategory.B, _rules, [TestBlock.Create(3, [topicId]).Value], null)
            .Value;
        var questions = Enumerable.Range(1, questionCount).Select(number => NewQuestion(topicId, $"Вопрос {number}")).ToArray();
        await SaveAsync([student, course, contract, template, .. questions]);

        return (contract, template, topicId, questions);
    }

    private static QuestionPick Pick(Guid topicId, int count, params Guid[] excluded) =>
        new(LicenseCategory.B, null, [topicId], count, excluded);

    private async Task<TestSession> StartAsync(Contract contract, TestTemplate template, Guid topicId, Guid? examId = null)
    {
        await using var context = database.CreateContext();
        var drawn = await new QuestionPicker(context).PickAsync(Pick(topicId, 3), CancellationToken);

        return TestSession.Start(contract, template, [drawn], examId, FixedClock.Default.UtcNow).Value;
    }

    [Fact]
    public async Task Save_SessionWithAMistakeAndAnAddedQuestion_StoresTheAttemptAsShown()
    {
        var (contract, template, topicId, _) = await SceneAsync(5);
        var session = await StartAsync(contract, template, topicId);
        await SaveAsync(session);
        var answeredAt = FixedClock.Default.UtcNow.AddMinutes(1);

        await using (var context = database.CreateContext())
        {
            var tracked = await new TestSessionRepository(context).GetByIdAsync(session.Id, CancellationToken);
            var first = tracked!.Questions[0];
            var wrong = first.Options.First(option => option.OptionId != first.CorrectOptionId).OptionId;
            tracked.Answer(first.Id, wrong, answeredAt).IsSuccess.Should().BeTrue();
            tracked.PendingAddOnBlocks.Should().Equal(1);
            var shown = tracked.Questions.Select(question => question.QuestionId).ToArray();
            var extra = await new QuestionPicker(context).PickAsync(Pick(topicId, 1, shown), CancellationToken);
            tracked.AddOnQuestions(1, extra).IsSuccess.Should().BeTrue();
            await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
        }

        await using var reader = database.CreateContext();
        var loaded = await new TestSessionRepository(reader).GetByIdAsync(session.Id, CancellationToken);
        loaded!.Rules.Should().Be(_rules);
        loaded.Blocks.Should().Equal(template.Blocks);
        loaded.PendingAddOnBlocks.Should().BeEmpty();
        loaded.DeadlineUtc.Should().Be(FixedClock.Default.UtcNow.AddMinutes(15));
        loaded.Questions.Select(question => question.Position).Should().Equal(1, 2, 3, 4);
        loaded.Questions.Select(question => question.IsAddOn).Should().Equal(false, false, false, true);
        loaded.Questions.Select(question => question.QuestionId).Should().OnlyHaveUniqueItems();
        loaded.Questions[0].IsCorrect.Should().BeFalse();
        loaded.Questions[0].AnsweredAtUtc.Should().Be(answeredAt);
        loaded.Questions[0].Options.Select(option => option.Text)
            .Should().Equal("Остановиться", "Продолжить движение", "Подать сигнал");
        loaded.MainMistakeCount.Should().Be(1);
    }

    [Fact]
    public async Task Queries_ReadTheAttemptsOfAContractWithTheirCounts()
    {
        var (contract, template, topicId, _) = await SceneAsync(4);
        var session = await StartAsync(contract, template, topicId);
        var first = session.Questions[0];
        session.Answer(first.Id, first.CorrectOptionId, FixedClock.Default.UtcNow.AddMinutes(1)).IsSuccess.Should().BeTrue();
        var second = session.Questions[1];
        var wrong = second.Options.First(option => option.OptionId != second.CorrectOptionId).OptionId;
        session.Answer(second.Id, wrong, FixedClock.Default.UtcNow.AddMinutes(2)).IsSuccess.Should().BeTrue();
        await SaveAsync(session);
        await using var context = database.CreateContext();
        var queries = new TestSessionQueries(context);

        var list = await queries.GetOfContractAsync(contract.Id, CancellationToken);
        var details = await queries.GetDetailsAsync(session.Id, CancellationToken);

        list!.StudentId.Should().Be(contract.StudentId);
        var item = list.Sessions.Should().ContainSingle().Which;
        item.TemplateName.Should().Be(template.Name);
        item.Status.Should().Be(TestSessionStatus.InProgress);
        item.QuestionCount.Should().Be(3);
        item.CorrectAnswerCount.Should().Be(1);
        item.MistakeCount.Should().Be(1);
        details!.Session.Should().Be(item);
        details.ContractId.Should().Be(contract.Id);
        details.Questions.Select(question => question.IsCorrect).Should().Equal(true, false, null);
        details.Questions[0].Options.Select(option => option.Text)
            .Should().Equal("Остановиться", "Продолжить движение", "Подать сигнал");
        (await queries.GetOfContractAsync(Guid.CreateVersion7(), CancellationToken)).Should().BeNull();
        (await queries.GetDetailsAsync(Guid.CreateVersion7(), CancellationToken)).Should().BeNull();
    }

    [Fact]
    public async Task Save_SecondAttemptInProgressOfAContract_ThrowsConcurrencyConflict()
    {
        var (contract, template, topicId, _) = await SceneAsync(3);
        var examId = Guid.CreateVersion7();
        var first = await StartAsync(contract, template, topicId, examId);
        await SaveAsync(first);

        var save = async () => await SaveAsync(await StartAsync(contract, template, topicId));

        await save.Should().ThrowAsync<ConcurrencyConflictException>();
        await using var context = database.CreateContext();
        var repository = new TestSessionRepository(context);
        var inProgress = await repository.GetInProgressOfContractAsync(contract.Id, CancellationToken);
        inProgress!.Id.Should().Be(first.Id);
        inProgress.Questions.Should().HaveCount(3);
        (await repository.ExistsForExamAsync(examId, contract.Id, CancellationToken)).Should().BeTrue();
        (await repository.ExistsForExamAsync(Guid.CreateVersion7(), contract.Id, CancellationToken)).Should().BeFalse();
    }

    [Fact]
    public async Task Pick_DrawsOnlyActiveQuestionsOfTheCategoryAndTopicThatAreNotExcluded()
    {
        var (_, _, topicId, questions) = await SceneAsync(3);
        var inactive = NewQuestion(topicId, "Снятый вопрос");
        inactive.Deactivate().IsSuccess.Should().BeTrue();
        var otherCategory = NewQuestion(topicId, "Вопрос категории C", LicenseCategory.C);
        await SaveAsync(inactive, otherCategory);
        await using var context = database.CreateContext();
        var picker = new QuestionPicker(context);

        var drawn = await picker.PickAsync(Pick(topicId, 10, questions[0].Id), CancellationToken);
        var one = await picker.PickAsync(Pick(topicId, 1), CancellationToken);
        var none = await picker.PickAsync(Pick(topicId, 0), CancellationToken);

        drawn.Select(question => question.Id).Should().BeEquivalentTo(new[] { questions[1].Id, questions[2].Id });
        drawn.Should().AllSatisfy(question => question.Options.Select(option => option.Text)
            .Should().Equal("Остановиться", "Продолжить движение", "Подать сигнал"));
        one.Should().ContainSingle();
        questions.Select(question => question.Id).Should().Contain(one[0].Id);
        none.Should().BeEmpty();
    }
}
