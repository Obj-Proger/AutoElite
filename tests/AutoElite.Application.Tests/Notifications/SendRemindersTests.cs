using System.Reflection;
using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Notifications.SendReminders;
using AutoElite.Application.Settings;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Notifications;

public class SendRemindersTests
{
    private static readonly DateTime _now = TestData.Now;

    /// <summary>A lesson whose day-ahead reminder became due ten minutes ago; 12:50 in the branch's time zone.</summary>
    private static readonly DateTime _dayAheadStart = TestData.Now.AddHours(23).AddMinutes(50);

    private readonly NotificationFixture _fixture = new();
    private readonly FakeDrivingSlotRepository _slots = new();
    private readonly FakeTheoryLessonRepository _lessons = new();
    private readonly FakeStudyGroupRepository _groups = new();
    private readonly FakeExamRepository _exams = new();
    private readonly User _student;
    private readonly User _instructor;
    private readonly Contract _contract;

    public SendRemindersTests()
    {
        _student = _fixture.SeedUser("student@example.com");
        _instructor = _fixture.SeedUser("instructor@example.com", language: Language.English);
        _contract = _fixture.SeedContract(_student, _fixture.Branch.Id);
    }

    private Task<Result> Run(DateTime? utcNow = null) =>
        new SendRemindersCommandHandler(
                new SchoolSettingsProvider(_fixture.Settings), _slots, _lessons, _exams, _fixture.Contracts, _groups,
                _fixture.Notifications, _fixture.CreateLocalTimes(), _fixture.CreateNotifier(),
                new FixedTimeProvider(new DateTimeOffset(utcNow ?? _now)))
            .Handle(new SendRemindersCommand(), TestContext.Current.CancellationToken);

    /// <summary>Seeds a driving lesson booked at <paramref name="bookedAtUtc"/>; reminders are 24 and 2 hours ahead.</summary>
    private DrivingSlot SeedBookedSlot(DateTime startUtc, DateTime bookedAtUtc)
    {
        var vehicle = Vehicle.Register(
                LicensePlate.Create("A123BC66").Value, "Lada", "Vesta", 2024, LicenseCategory.B, Transmission.Manual, null, _now)
            .Value;
        var slot = DrivingSlot.Offer(
                _instructor.Id, vehicle, null, null, TimeInterval.Create(startUtc, startUtc.AddHours(2)).Value, bookedAtUtc.AddHours(-1))
            .Value;
        slot.Book(_contract, bookedAtUtc).IsSuccess.Should().BeTrue();
        _slots.Seed(slot);

        return slot;
    }

    [Fact]
    public void Command_IsReservedForTheSystem()
    {
        typeof(SendRemindersCommand).GetCustomAttribute<SystemRequestAttribute>().Should().NotBeNull();
    }

    [Fact]
    public async Task DrivingLesson_DayAheadReminderDue_RemindsTheStudentAndTheInstructor()
    {
        var slot = SeedBookedSlot(_dayAheadStart, _now.AddHours(-2));

        var result = await Run();

        result.IsSuccess.Should().BeTrue();
        _fixture.Notifications.Items.Select(notification => notification.RecipientId)
            .Should().BeEquivalentTo<Guid>([_student.Id, _instructor.Id]);
        var reminder = _fixture.Of(_student.Id).Single();
        reminder.Type.Should().Be(NotificationType.LessonReminder);
        reminder.Title.Should().Be("Напоминание о занятии");
        reminder.Body.Should().Be("Занятие по вождению 2 марта в 12:50.");
        reminder.Subject.Should().Be(EntityReference.Create(EntityKind.DrivingSlot, slot.Id).Value);
        reminder.CreatedAtUtc.Should().Be(_now);
        _fixture.Of(_instructor.Id).Single().Body.Should().Be("Your driving lesson is on March 2 at 12:50 PM.");
    }

    [Fact]
    public async Task DrivingLesson_RunAgain_DoesNotRemindTwice()
    {
        SeedBookedSlot(_dayAheadStart, _now.AddHours(-2));
        await Run();

        await Run(_now.AddMinutes(5));

        _fixture.Notifications.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task DrivingLesson_TwoHourMomentLater_SendsTheSecondReminder()
    {
        SeedBookedSlot(_dayAheadStart, _now.AddHours(-2));
        await Run();

        await Run(_dayAheadStart.AddHours(-1.5));

        _fixture.Of(_student.Id).Should().HaveCount(2);
    }

    [Fact]
    public async Task DrivingLesson_BookedAfterTheMoment_RemindsNobody()
    {
        SeedBookedSlot(_now.AddHours(1.5), _now.AddMinutes(-10));

        await Run();

        _fixture.Notifications.Items.Should().BeEmpty("the student booked the lesson after the two-hour moment");
    }

    [Fact]
    public async Task TheoryLesson_RemindsTheInstructorAndTheStudentsWhoJoinedBeforeTheMoment()
    {
        var course = TestData.NewCourse();
        var topicId = course.AddTopic("Road signs", null).Value.Id;
        var group = StudyGroup.Create(
                "B-12", course, _instructor.Id, _fixture.Branch.Id, new DateOnly(2026, 2, 1), new DateOnly(2026, 6, 30), 20)
            .Value;
        _groups.Seed(group);
        group.Enroll(_fixture.SeedContract(_student, _fixture.Branch.Id, course), _now.AddDays(-1)).IsSuccess.Should().BeTrue();
        var latecomer = _fixture.SeedUser("late@example.com");
        group.Enroll(_fixture.SeedContract(latecomer, _fixture.Branch.Id, course), _now).IsSuccess.Should().BeTrue();
        var start = _now.AddMinutes(90);
        _lessons.Seed(TheoryLesson.Schedule(
                group, course, topicId, _instructor.Id, TimeInterval.Create(start, start.AddHours(2)).Value, _now.AddDays(-1))
            .Value);

        await Run();

        _fixture.Notifications.Items.Select(notification => notification.RecipientId)
            .Should().BeEquivalentTo<Guid>([_student.Id, _instructor.Id]);
        _fixture.Of(_student.Id).Single().Body.Should().Be("Занятие группы «B-12» 1 марта в 14:30.");
    }

    [Fact]
    public async Task RemindersTurnedOff_RemindsNobody()
    {
        var settings = SchoolSettings.CreateDefault(Currency.Create("RUB").Value);
        settings.Update(settings.DefaultCurrency, settings.TimeZoneId, 10, 2, TimeSpan.FromHours(24), []).IsSuccess.Should().BeTrue();
        _fixture.Settings.Settings = settings;
        SeedBookedSlot(_dayAheadStart, _now.AddHours(-2));

        await Run();

        _fixture.Notifications.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Exam_RemindsTheStudentsAndTheResponsibleInstructorOnce()
    {
        var course = TestData.NewCourse();
        var group = StudyGroup.Create(
                "B-12", course, _instructor.Id, _fixture.Branch.Id, new DateOnly(2026, 2, 1), new DateOnly(2026, 6, 30), 20)
            .Value;
        _groups.Seed(group);
        group.Enroll(_fixture.SeedContract(_student, _fixture.Branch.Id, course), _now.AddDays(-1)).IsSuccess.Should().BeTrue();
        var exam = Exam.Schedule(group, ExamKind.Practice, null, _dayAheadStart, _now.AddDays(-1)).Value;
        _exams.Seed(exam);

        await Run();
        await Run(_now.AddMinutes(5));

        _fixture.Notifications.Items.Select(notification => notification.RecipientId)
            .Should().BeEquivalentTo<Guid>([_student.Id, _instructor.Id]);
        var reminder = _fixture.Of(_student.Id).Single();
        reminder.Type.Should().Be(NotificationType.ExamReminder);
        reminder.Title.Should().Be("Напоминание об экзамене");
        reminder.Body.Should().Be("Практический экзамен группы «B-12» — 2 марта в 12:50.");
        reminder.Subject.Should().Be(EntityReference.Create(EntityKind.Exam, exam.Id).Value);
    }

    [Fact]
    public async Task Exam_Cancelled_RemindsNobody()
    {
        var course = TestData.NewCourse();
        var group = StudyGroup.Create(
                "B-12", course, _instructor.Id, _fixture.Branch.Id, new DateOnly(2026, 2, 1), new DateOnly(2026, 6, 30), 20)
            .Value;
        _groups.Seed(group);
        var exam = Exam.Schedule(group, ExamKind.Practice, null, _dayAheadStart, _now.AddDays(-1)).Value;
        exam.Cancel("Examiner unavailable", _now.AddHours(-1)).IsSuccess.Should().BeTrue();
        _exams.Seed(exam);

        await Run();

        _fixture.Notifications.Items.Should().BeEmpty();
    }
}
