using AutoElite.Application.Notifications;
using AutoElite.Application.Notifications.Exams;
using AutoElite.Application.Notifications.StudyGroups;
using AutoElite.Application.Notifications.TheoryLessons;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;

namespace AutoElite.Application.Tests.Notifications;

public class GroupNotificationTests
{
    /// <summary>13:00 in the branch's time zone, five hours ahead of UTC.</summary>
    private static readonly DateTime _start = new(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc);

    private readonly NotificationFixture _fixture = new();
    private readonly FakeStudyGroupRepository _groups = new();
    private readonly InMemoryRepository<Exam> _exams = new();
    private readonly StudyGroup _group;
    private readonly User _student;
    private readonly User _withdrawn;

    public GroupNotificationTests()
    {
        var course = TestData.NewCourse();
        _group = StudyGroup.Create(
                "B-12", course, Guid.CreateVersion7(), _fixture.Branch.Id, new DateOnly(2026, 3, 2), new DateOnly(2026, 6, 30), 20)
            .Value;
        _groups.Seed(_group);

        _student = _fixture.SeedUser("student@example.com");
        _group.Enroll(_fixture.SeedContract(_student, _fixture.Branch.Id, course), TestData.Now).IsSuccess.Should().BeTrue();

        _withdrawn = _fixture.SeedUser("withdrawn@example.com", language: Language.English);
        var withdrawnContract = _fixture.SeedContract(_withdrawn, _fixture.Branch.Id, course);
        _group.Enroll(withdrawnContract, TestData.Now);
        _group.Withdraw(withdrawnContract.Id, TestData.Now);
    }

    private GroupNotifier CreateGroupNotifier() => new(_groups, _fixture.CreateLocalTimes(), _fixture.CreateNotifier());

    private Exam SeedExam()
    {
        var exam = Exam.Schedule(_group, ExamKind.Practice, null, _start, TestData.Now).Value;
        _exams.Seed(exam);

        return exam;
    }

    [Fact]
    public async Task TheoryLessonCancelled_NotifiesTheCurrentStudentsWithTheReason()
    {
        var lessonId = Guid.CreateVersion7();

        await new NotifyGroupWhenTheoryLessonCancelledHandler(CreateGroupNotifier()).Handle(
            new TheoryLessonCancelledDomainEvent(lessonId, _group.Id, _start, "Instructor is ill", TestData.Now),
            TestContext.Current.CancellationToken);

        var notification = _fixture.Notifications.Items.Should().ContainSingle().Which;
        notification.RecipientId.Should().Be(_student.Id);
        notification.Type.Should().Be(NotificationType.TheoryLessonChanged);
        notification.Body.Should().Be("Занятие группы «B-12» 10 марта в 13:00 отменено. Причина: Instructor is ill");
        notification.Subject.Should().Be(EntityReference.Create(EntityKind.TheoryLesson, lessonId).Value);
    }

    [Fact]
    public async Task TheoryLessonRescheduled_GroupWithoutBranch_ShowsTheTimeInTheSchoolsTimeZone()
    {
        var course = TestData.NewCourse();
        var group = StudyGroup.Create(
                "B-13", course, Guid.CreateVersion7(), null, new DateOnly(2026, 3, 2), new DateOnly(2026, 6, 30), 20)
            .Value;
        _groups.Seed(group);
        var student = _fixture.SeedUser("other@example.com");
        group.Enroll(_fixture.SeedContract(student, null, course), TestData.Now).IsSuccess.Should().BeTrue();

        await new NotifyGroupWhenTheoryLessonRescheduledHandler(CreateGroupNotifier()).Handle(
            new TheoryLessonRescheduledDomainEvent(Guid.CreateVersion7(), group.Id, _start, _start.AddHours(2), TestData.Now),
            TestContext.Current.CancellationToken);

        _fixture.Of(student.Id).Should().ContainSingle()
            .Which.Body.Should().Be("Занятие группы «B-13» перенесено на 10 марта в 11:00.");
    }

    [Fact]
    public async Task ExamScheduled_NamesTheKindOfTheExam()
    {
        await new NotifyGroupWhenExamScheduledHandler(CreateGroupNotifier()).Handle(
            new ExamScheduledDomainEvent(Guid.CreateVersion7(), _group.Id, ExamKind.Theory, _start, TestData.Now),
            TestContext.Current.CancellationToken);

        var notification = _fixture.Of(_student.Id).Should().ContainSingle().Which;
        notification.Type.Should().Be(NotificationType.ExamChanged);
        notification.Title.Should().Be("Назначен экзамен");
        notification.Body.Should().Be("Теоретический экзамен группы «B-12» назначен на 10 марта в 13:00.");
    }

    [Fact]
    public async Task ExamRescheduled_ShowsTheNewTime()
    {
        var exam = SeedExam();
        exam.Reschedule(_start.AddDays(1), TestData.Now);

        await new NotifyGroupWhenExamRescheduledHandler(_exams, CreateGroupNotifier()).Handle(
            exam.DomainEvents.OfType<ExamRescheduledDomainEvent>().Single(), TestContext.Current.CancellationToken);

        _fixture.Of(_student.Id).Should().ContainSingle()
            .Which.Body.Should().Be("Практический экзамен группы «B-12» перенесён на 11 марта в 13:00.");
    }

    [Fact]
    public async Task ExamCancelled_ShowsTheTimeAndTheReason()
    {
        var exam = SeedExam();
        exam.Cancel("Examiner unavailable", TestData.Now);

        await new NotifyGroupWhenExamCancelledHandler(_exams, CreateGroupNotifier()).Handle(
            exam.DomainEvents.OfType<ExamCancelledDomainEvent>().Single(), TestContext.Current.CancellationToken);

        _fixture.Of(_student.Id).Should().ContainSingle()
            .Which.Body.Should().Be("Практический экзамен группы «B-12» 10 марта в 13:00 отменён. Причина: Examiner unavailable");
    }

    [Theory]
    [InlineData(true, "Экзамен сдан", "Теоретический экзамен сдан. Поздравляем!")]
    [InlineData(false, "Экзамен не сдан", "Теоретический экзамен не сдан.")]
    public async Task ExamResultRecorded_TellsTheStudentWhetherTheyPassed(bool passed, string title, string body)
    {
        var examId = Guid.CreateVersion7();

        await new NotifyWhenExamResultRecordedHandler(_fixture.CreateNotifier()).Handle(
            new ExamResultRecordedDomainEvent(examId, Guid.CreateVersion7(), _student.Id, ExamKind.Theory, passed, TestData.Now),
            TestContext.Current.CancellationToken);

        var notification = _fixture.Of(_student.Id).Should().ContainSingle().Which;
        notification.Type.Should().Be(NotificationType.ExamResult);
        notification.Title.Should().Be(title);
        notification.Body.Should().Be(body);
        notification.Subject.Should().Be(EntityReference.Create(EntityKind.Exam, examId).Value);
    }

    [Fact]
    public async Task StudentEnrolled_TellsTheStudentTheGroupAndItsStart()
    {
        await new NotifyWhenStudentEnrolledHandler(_groups, _fixture.CreateNotifier()).Handle(
            _group.DomainEvents.OfType<StudentEnrolledDomainEvent>().First(), TestContext.Current.CancellationToken);

        var notification = _fixture.Of(_student.Id).Should().ContainSingle().Which;
        notification.Type.Should().Be(NotificationType.EnrolledInGroup);
        notification.Body.Should().Be("Группа «B-12». Начало занятий — 2 марта 2026.");
    }

    [Fact]
    public async Task StudentEnrolled_WithdrawnBeforeTheEventIsHandled_NotifiesNobody()
    {
        await new NotifyWhenStudentEnrolledHandler(_groups, _fixture.CreateNotifier()).Handle(
            _group.DomainEvents.OfType<StudentEnrolledDomainEvent>().Last(), TestContext.Current.CancellationToken);

        _fixture.Of(_withdrawn.Id).Should().BeEmpty();
    }
}
