using AutoElite.Application.StudentCards;
using AutoElite.Domain.Enums;
using AutoElite.Infrastructure.Persistence.Queries;

namespace AutoElite.Infrastructure.Tests.Persistence;

public sealed class StudentCardQueriesTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static readonly DateTime _now = FixedClock.Default.UtcNow;

    private static readonly DateTime _april = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>The time the card is read: after the lessons of early April and before the late ones.</summary>
    private static readonly DateTime _cardAt = _april.AddDays(19);

    private static readonly AnswerOptionDraft[] _options = [new("Да", true), new("Нет", false)];

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static TimeInterval Hours(int day, int fromHour, int toHour) =>
        TimeInterval.Create(_april.AddDays(day).AddHours(fromHour), _april.AddDays(day).AddHours(toHour)).Value;

    private async Task SaveAsync(params object[] records)
    {
        await using var context = database.CreateContext();
        context.AddRange(records);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
    }

    private static StudentCardQueries Queries(AutoEliteDbContext context) =>
        new(context, new ContractQueries(context), new TestSessionQueries(context), new ExamQueries(context));

    [Fact]
    public async Task Get_StudentWithAContract_SummarizesTheoryDrivingTestsAndExams()
    {
        var instructor = TrainingRecords.Person("Орлов");
        var student = TrainingRecords.Person("Кузнецов");
        var course = TrainingRecords.NewCourse();
        var topicId = course.AddTopic("Дорожные знаки", null).Value.Id;
        var contract = TrainingRecords.NewContract(TrainingRecords.Number(), student, course);
        var group = TrainingRecords.NewGroup(course, instructor);
        group.Enroll(contract, _now).IsSuccess.Should().BeTrue();

        var attended = TheoryLesson.Schedule(group, course, topicId, instructor.Id, Hours(1, 10, 12), _now).Value;
        attended.MarkAttendance(group, contract, true, null, instructor.Id, _april.AddDays(1).AddHours(12))
            .IsSuccess.Should().BeTrue();
        var missed = TheoryLesson.Schedule(group, course, topicId, instructor.Id, Hours(2, 10, 12), _now).Value;
        var cancelled = TheoryLesson.Schedule(group, course, topicId, instructor.Id, Hours(3, 10, 12), _now).Value;
        cancelled.Cancel("Болезнь преподавателя", _now).IsSuccess.Should().BeTrue();
        var upcoming = TheoryLesson.Schedule(group, course, topicId, instructor.Id, Hours(25, 10, 12), _now).Value;

        var vehicle = Vehicle.Register(
                LicensePlate.Create("ВС" + string.Concat(Guid.NewGuid().ToString("N").Where(char.IsDigit).Take(8))).Value,
                "Lada", "Vesta", 2024, LicenseCategory.B, Transmission.Manual, null, _now)
            .Value;
        DrivingSlot Slot(int day) => DrivingSlot.Offer(instructor.Id, vehicle, null, null, Hours(day, 9, 11), _now).Value;
        var completed = Slot(5);
        completed.Book(contract, _now).IsSuccess.Should().BeTrue();
        completed.Complete(contract, 2m, [], null, 10, _april.AddDays(5).AddHours(11)).IsSuccess.Should().BeTrue();
        completed.Rate(4, null, 2, _april.AddDays(5).AddHours(12)).IsSuccess.Should().BeTrue();
        var noShow = Slot(6);
        noShow.Book(contract, _now).IsSuccess.Should().BeTrue();
        noShow.MarkNoShow(contract, null, _april.AddDays(6).AddHours(9)).IsSuccess.Should().BeTrue();
        var booked = Slot(26);
        booked.Book(contract, _now).IsSuccess.Should().BeTrue();

        var rules = TestRules.Create(1, TimeSpan.FromMinutes(10), 0, null, 0, TimeSpan.Zero).Value;
        var template = TestTemplate.Create(
                $"Тренировка {Guid.NewGuid():N}", LicenseCategory.B, rules, [TestBlock.Create(1, [topicId]).Value], null)
            .Value;
        var question = Question.Create("Вопрос", null, topicId, [LicenseCategory.B], QuestionSource.School, null, _options).Value;
        var passed = TestSession.Start(contract, template, [[question]], null, _april.AddDays(7)).Value;
        passed.Answer(passed.Questions[0].Id, passed.Questions[0].CorrectOptionId, _april.AddDays(7).AddMinutes(1))
            .IsSuccess.Should().BeTrue();
        var inProgress = TestSession.Start(contract, template, [[question]], null, _april.AddDays(8)).Value;
        var exam = Exam.Schedule(group, ExamKind.Practice, null, _april.AddDays(27), _now).Value;

        await SaveAsync(instructor, student, course, contract, group, vehicle, template, question);
        await SaveAsync(attended, missed, cancelled, upcoming, completed, noShow, booked, passed, exam);
        await SaveAsync(inProgress);
        await using var context = database.CreateContext();

        var card = await Queries(context).GetAsync(student.Id, _cardAt, CancellationToken);

        card!.LastName.Should().Be("Кузнецов");
        card.Email.Should().Be(student.Email.Value);
        card.IsActive.Should().BeTrue();
        var entry = card.Contracts.Should().ContainSingle().Which;
        entry.Contract.Id.Should().Be(contract.Id);
        entry.Contract.CompletedPracticeHours.Should().Be(2m);
        entry.Group.Should().Be(new StudentCardGroup(group.Id, group.Name, 2, 1));
        entry.Group!.LessonsMissed.Should().Be(1);
        entry.Driving.Should().Be(new StudentCardDriving(1, 1, 1, 4m));
        entry.Tests.Attempts.Should().Be(2);
        entry.Tests.Passed.Should().Be(1);
        entry.Tests.Failed.Should().Be(0);
        entry.Tests.LastAttempt!.Id.Should().Be(inProgress.Id);
        entry.Tests.LastAttempt.CorrectAnswerCount.Should().BeNull();
        entry.Tests.LastAttempt.MistakeCount.Should().BeNull();
        entry.Exams.Should().ContainSingle().Which.Exam.Id.Should().Be(exam.Id);
    }

    [Fact]
    public async Task Get_StudentWithoutContractsOrUnknown_ReturnsAnEmptyCardOrNothing()
    {
        var student = TrainingRecords.Person("Новиков");
        await SaveAsync(student);
        await using var context = database.CreateContext();
        var queries = Queries(context);

        var card = await queries.GetAsync(student.Id, _cardAt, CancellationToken);
        var unknown = await queries.GetAsync(Guid.CreateVersion7(), _cardAt, CancellationToken);

        card!.Contracts.Should().BeEmpty();
        unknown.Should().BeNull();
    }
}
