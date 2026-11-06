using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Exams;
using AutoElite.Domain.Enums;
using AutoElite.Infrastructure.Persistence.Queries;
using AutoElite.Infrastructure.Persistence.Repositories;

namespace AutoElite.Infrastructure.Tests.Persistence;

public sealed class ExamPersistenceTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static readonly DateTime _now = FixedClock.Default.UtcNow;

    private static readonly AnswerOptionDraft[] _options = [new("Остановиться", true), new("Продолжить движение", false)];

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task SaveAsync(params object[] records)
    {
        await using var context = database.CreateContext();
        context.AddRange(records);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
    }

    /// <summary>
    /// A group of two students with a theory exam in an hour and a practical exam in two hours, and the questions of
    /// the theory exam's template, saved.
    /// </summary>
    private async Task<Scene> SceneAsync()
    {
        var instructor = TrainingRecords.Person("Орлов");
        var first = TrainingRecords.Person("Андреев");
        var second = TrainingRecords.Person("Борисов");
        var course = TrainingRecords.NewCourse();
        var topicId = course.AddTopic("Дорожные знаки", null).Value.Id;
        var branch = TrainingRecords.NewBranch();
        var firstContract = TrainingRecords.NewContract(TrainingRecords.Number(), first, course, branch.Id);
        var secondContract = TrainingRecords.NewContract(TrainingRecords.Number(), second, course, branch.Id);
        var group = TrainingRecords.NewGroup(course, instructor, branch.Id);
        group.Enroll(firstContract, _now).IsSuccess.Should().BeTrue();
        group.Enroll(secondContract, _now).IsSuccess.Should().BeTrue();
        var rules = TestRules.Create(2, TimeSpan.FromMinutes(10), 1, null, 0, TimeSpan.Zero).Value;
        var template = TestTemplate.Create(
                $"Экзамен {Guid.NewGuid():N}", LicenseCategory.B, rules, [TestBlock.Create(2, [topicId]).Value], null)
            .Value;
        Question[] questions =
        [
            .. Enumerable.Range(1, 2).Select(number => Question.Create(
                    $"Вопрос {number}", null, topicId, [LicenseCategory.B], QuestionSource.School, null, _options)
                .Value)
        ];
        var theory = Exam.Schedule(group, ExamKind.Theory, template, _now.AddHours(1), _now).Value;
        var practice = Exam.Schedule(group, ExamKind.Practice, null, _now.AddHours(2), _now).Value;
        await SaveAsync(
            [instructor, first, second, course, branch, firstContract, secondContract, group, template, .. questions, theory, practice]);

        return new Scene(instructor, group, firstContract, secondContract, template, questions, theory, practice);
    }

    /// <summary>A passed attempt of the first student at the theory exam, saved.</summary>
    private async Task<TestSession> PassTheoryAsync(Scene scene)
    {
        var startedAt = _now.AddHours(1);
        var session = TestSession.Start(scene.FirstContract, scene.Template, [scene.Questions], scene.Theory.Id, startedAt).Value;
        foreach (var question in session.Questions)
            session.Answer(question.Id, question.CorrectOptionId, startedAt.AddMinutes(1)).IsSuccess.Should().BeTrue();

        session.Status.Should().Be(TestSessionStatus.Passed);
        await SaveAsync(session);

        return session;
    }

    /// <summary>Loads an exam and the group, changes the exam and saves it.</summary>
    private async Task ChangeAsync(Guid examId, Guid groupId, Action<Exam, StudyGroup> change)
    {
        await using var context = database.CreateContext();
        var exam = await new ExamRepository(context).GetByIdAsync(examId, CancellationToken);
        var group = await new StudyGroupRepository(context).GetByIdAsync(groupId, CancellationToken);
        change(exam!, group!);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
    }

    [Fact]
    public async Task Save_TheoryResultAndItsCorrection_StoresTheResult()
    {
        var scene = await SceneAsync();
        var session = await PassTheoryAsync(scene);

        await ChangeAsync(scene.Theory.Id, scene.Group.Id, (exam, group) =>
            exam.RecordTheoryResult(group, session, _now.AddHours(2)).IsSuccess.Should().BeTrue());
        await ChangeAsync(scene.Theory.Id, scene.Group.Id, (exam, _) =>
            exam.CorrectResult(scene.FirstContract.Id, false, 1, "Пересмотрено", scene.Instructor.Id, _now.AddHours(3))
                .IsSuccess.Should().BeTrue());

        await using var context = database.CreateContext();
        var loaded = await new ExamRepository(context).GetByIdAsync(scene.Theory.Id, CancellationToken);
        var result = loaded!.Results.Should().ContainSingle().Which;
        result.ContractId.Should().Be(scene.FirstContract.Id);
        result.TestSessionId.Should().Be(session.Id);
        result.RecordedByUserId.Should().BeNull();
        result.RecordedAtUtc.Should().Be(_now.AddHours(2));
        result.Passed.Should().BeFalse();
        result.Score.Should().Be(1);
        result.Comment.Should().Be("Пересмотрено");
        result.CorrectedByUserId.Should().Be(scene.Instructor.Id);
        result.CorrectedAtUtc.Should().Be(_now.AddHours(3));
        loaded.TestTemplateId.Should().Be(scene.Template.Id);
        loaded.Status.Should().Be(ExamStatus.Scheduled);
    }

    [Fact]
    public async Task Save_TwoResultsOfAContractAtOnce_ThrowsConcurrencyConflict()
    {
        var scene = await SceneAsync();
        await using var first = database.CreateContext();
        await using var second = database.CreateContext();
        foreach (var context in new[] { first, second })
        {
            var exam = await new ExamRepository(context).GetByIdAsync(scene.Practice.Id, CancellationToken);
            var group = await new StudyGroupRepository(context).GetByIdAsync(scene.Group.Id, CancellationToken);
            exam!.RecordPracticeResult(group!, scene.SecondContract, true, 90, null, scene.Instructor.Id, _now.AddHours(3))
                .IsSuccess.Should().BeTrue();
        }

        await new UnitOfWork(first).SaveChangesAsync(CancellationToken);
        var save = () => new UnitOfWork(second).SaveChangesAsync(CancellationToken);

        await save.Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task Queries_ReadTheScheduleTheGroupTheStudentAndTheCard()
    {
        var scene = await SceneAsync();
        await ChangeAsync(scene.Practice.Id, scene.Group.Id, (exam, group) =>
            exam.RecordPracticeResult(group, scene.SecondContract, true, 90, "Уверенно", scene.Instructor.Id, _now.AddHours(3))
                .IsSuccess.Should().BeTrue());
        await using var context = database.CreateContext();
        var queries = new ExamQueries(context);
        var day = TimeInterval.Create(_now, _now.AddDays(1)).Value;
        var branchScope = DataScope.ForBranch(scene.Group.BranchId!.Value);

        var schedule = await queries.GetScheduleAsync(
            new ExamScheduleFilter(day, null, null, scene.Instructor.Id), branchScope, CancellationToken);
        var practiceOnly = await queries.GetScheduleAsync(
            new ExamScheduleFilter(day, ExamKind.Practice, scene.Group.Id, null), DataScope.SchoolWide, CancellationToken);
        var otherBranch = await queries.GetScheduleAsync(
            new ExamScheduleFilter(day, null, null, scene.Instructor.Id), DataScope.ForBranch(Guid.CreateVersion7()),
            CancellationToken);
        var afterPractice = TimeInterval.Create(_now.AddHours(2).AddMinutes(1), _now.AddDays(1)).Value;
        var later = await queries.GetScheduleAsync(
            new ExamScheduleFilter(afterPractice, null, scene.Group.Id, null), DataScope.SchoolWide, CancellationToken);
        var ofGroup = await queries.GetOfGroupAsync(scene.Group.Id, CancellationToken);
        var ofStudent = await queries.GetOfStudentAsync(scene.SecondContract.StudentId, CancellationToken);
        var card = await queries.GetDetailsAsync(scene.Practice.Id, CancellationToken);

        schedule.Select(exam => exam.Id).Should().Equal(scene.Theory.Id, scene.Practice.Id);
        schedule[0].TestTemplateName.Should().Be(scene.Template.Name);
        schedule[1].TestTemplateName.Should().BeNull();
        schedule[1].ResultCount.Should().Be(1);
        practiceOnly.Should().ContainSingle().Which.Id.Should().Be(scene.Practice.Id);
        otherBranch.Should().BeEmpty();
        later.Should().BeEmpty();
        ofGroup!.ResponsibleInstructorId.Should().Be(scene.Instructor.Id);
        ofGroup.Exams.Select(exam => exam.Id).Should().Equal(scene.Theory.Id, scene.Practice.Id);
        ofStudent.Select(item => item.Exam.Id).Should().Equal(scene.Practice.Id, scene.Theory.Id);
        ofStudent[0].Result!.Score.Should().Be(90);
        ofStudent[1].Result.Should().BeNull();
        var result = card!.Results.Should().ContainSingle().Which;
        result.ContractNumber.Should().Be(scene.SecondContract.Number);
        result.StudentName.Should().Be("Борисов Иван Петрович");
        result.Comment.Should().Be("Уверенно");
        result.RecordedByUserId.Should().Be(scene.Instructor.Id);
        (await queries.GetOfGroupAsync(Guid.CreateVersion7(), CancellationToken)).Should().BeNull();
        (await queries.GetDetailsAsync(Guid.CreateVersion7(), CancellationToken)).Should().BeNull();
    }

    [Fact]
    public async Task GetScheduledBetween_ReturnsTheScheduledExamsInTheWindow()
    {
        var scene = await SceneAsync();
        await ChangeAsync(scene.Theory.Id, scene.Group.Id, (exam, _) =>
            exam.Cancel("Перенос программы", _now).IsSuccess.Should().BeTrue());
        await using var context = database.CreateContext();
        var repository = new ExamRepository(context);

        var exams = await repository.GetScheduledBetweenAsync(_now, _now.AddHours(2), CancellationToken);
        var beforePractice = await repository.GetScheduledBetweenAsync(_now, _now.AddHours(2).AddMinutes(-1), CancellationToken);

        exams.Select(exam => exam.Id).Should().Contain(scene.Practice.Id).And.NotContain(scene.Theory.Id);
        beforePractice.Select(exam => exam.Id).Should().NotContain(scene.Practice.Id);
    }

    private sealed record Scene(
        User Instructor,
        StudyGroup Group,
        Contract FirstContract,
        Contract SecondContract,
        TestTemplate Template,
        Question[] Questions,
        Exam Theory,
        Exam Practice);
}
