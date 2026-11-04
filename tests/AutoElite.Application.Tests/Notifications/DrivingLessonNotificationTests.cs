using AutoElite.Application.Notifications.DrivingLessons;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;

namespace AutoElite.Application.Tests.Notifications;

public class DrivingLessonNotificationTests
{
    /// <summary>The lesson starts at 13:00 in the branch's time zone, five hours ahead of UTC.</summary>
    private static readonly DateTime _start = new(2026, 3, 3, 8, 0, 0, DateTimeKind.Utc);

    private readonly NotificationFixture _fixture = new();
    private readonly FakeDrivingSlotRepository _slots = new();
    private readonly User _student;
    private readonly User _instructor;
    private readonly Contract _contract;
    private readonly DrivingSlot _slot;

    public DrivingLessonNotificationTests()
    {
        _student = _fixture.SeedUser("student@example.com", firstName: "Pavel");
        _instructor = _fixture.SeedUser("instructor@example.com", language: Language.English, firstName: "Oleg");
        _contract = _fixture.SeedContract(_student, _fixture.Branch.Id);
        var vehicle = Vehicle.Register(
                LicensePlate.Create("A123BC66").Value, "Lada", "Vesta", 2024, LicenseCategory.B, Transmission.Manual, null, TestData.Now)
            .Value;
        _slot = DrivingSlot.Offer(_instructor.Id, vehicle, null, null, TimeInterval.Create(_start, _start.AddHours(2)).Value, TestData.Now)
            .Value;
        _slot.Book(_contract, TestData.Now);
        _slots.Seed(_slot);
    }

    private DrivingLessonReader CreateReader() => new(_slots, _fixture.Contracts, _fixture.CreateLocalTimes());

    private TEvent Raised<TEvent>() => _slot.DomainEvents.OfType<TEvent>().Single();

    private void Complete() => _slot.Complete(_contract, 2m, [], null, 10, _start.AddHours(2));

    [Fact]
    public async Task Booked_NotifiesTheInstructorInTheirLanguageWithTheLocalStart()
    {
        await new NotifyInstructorWhenDrivingLessonBookedHandler(CreateReader(), _fixture.Users, _fixture.CreateNotifier())
            .Handle(Raised<DrivingLessonBookedDomainEvent>(), TestContext.Current.CancellationToken);

        var notification = _fixture.Notifications.Items.Should().ContainSingle().Which;
        notification.RecipientId.Should().Be(_instructor.Id);
        notification.Type.Should().Be(NotificationType.DrivingLessonBooked);
        notification.Title.Should().Be("New driving booking");
        notification.Body.Should().Be("Ivanov Pavel booked the lesson on March 3 at 1:00 PM.");
        notification.Subject.Should().Be(EntityReference.Create(EntityKind.DrivingSlot, _slot.Id).Value);
    }

    [Fact]
    public async Task Booked_CancelledBeforeTheEventIsHandled_NotifiesNobody()
    {
        var booked = Raised<DrivingLessonBookedDomainEvent>();
        _slot.CancelBooking(_student.Id, "Ill", TimeSpan.Zero, TestData.Now);

        await new NotifyInstructorWhenDrivingLessonBookedHandler(CreateReader(), _fixture.Users, _fixture.CreateNotifier())
            .Handle(booked, TestContext.Current.CancellationToken);

        _fixture.Notifications.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Cancelled_ByTheStudent_NotifiesOnlyTheInstructor()
    {
        _slot.CancelBooking(_student.Id, "Ill", TimeSpan.Zero, TestData.Now);

        await Cancelled();

        var notification = _fixture.Notifications.Items.Should().ContainSingle().Which;
        notification.RecipientId.Should().Be(_instructor.Id);
        notification.Body.Should().Be("The driving lesson on March 3 at 1:00 PM is cancelled. Reason: Ill");
    }

    [Fact]
    public async Task Cancelled_ByTheSystem_NotifiesTheStudentAndTheInstructor()
    {
        _slot.CancelBooking(null, "Contract closed", TimeSpan.Zero, TestData.Now);

        await Cancelled();

        _fixture.Notifications.Items.Select(notification => notification.RecipientId)
            .Should().BeEquivalentTo<Guid>([_student.Id, _instructor.Id]);
        _fixture.Of(_student.Id).Single().Body.Should().Be("Занятие по вождению 3 марта в 13:00 отменено. Причина: Contract closed");
    }

    [Fact]
    public async Task Completed_InvitesTheStudentToRateTheLesson()
    {
        Complete();

        await InviteToRate();

        var notification = _fixture.Of(_student.Id).Should().ContainSingle().Which;
        notification.Type.Should().Be(NotificationType.RateDrivingLesson);
        notification.Title.Should().Be("Оцените занятие");
        notification.Body.Should().StartWith("Как прошло занятие по вождению 3 марта в 13:00?");
    }

    [Fact]
    public async Task Completed_AlreadyRatedWhenTheEventIsHandled_DoesNotInvite()
    {
        Complete();
        _slot.Rate(5, null, 2, _start.AddHours(3));

        await InviteToRate();

        _fixture.Notifications.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task RatedLow_NotifiesTheManagersOfTheContractsBranch()
    {
        var ownManager = _fixture.SeedManager("own@example.com", _fixture.Branch.Id);
        var otherManager = _fixture.SeedManager("other@example.com", Guid.CreateVersion7());
        Complete();
        _slot.Rate(1, "Late", 2, _start.AddHours(3));

        await new NotifyManagersWhenDrivingLessonRatedLowHandler(CreateReader(), _fixture.Users, _fixture.Roles, _fixture.CreateNotifier())
            .Handle(Raised<LowDrivingLessonRatingDomainEvent>(), TestContext.Current.CancellationToken);

        var notification = _fixture.Notifications.Items.Should().ContainSingle().Which;
        notification.RecipientId.Should().Be(ownManager.Id);
        notification.Type.Should().Be(NotificationType.LowDrivingLessonRating);
        notification.Body.Should()
            .Be("Занятие по вождению 3 марта в 13:00 оценено на 1 из 5. Студент: Ivanov Pavel. Инструктор: Ivanov Oleg.");
        _fixture.Of(otherManager.Id).Should().BeEmpty();
    }

    private Task Cancelled() =>
        new NotifyWhenDrivingBookingCancelledHandler(CreateReader(), _fixture.CreateNotifier())
            .Handle(Raised<DrivingBookingCancelledDomainEvent>(), TestContext.Current.CancellationToken);

    private Task InviteToRate() =>
        new InviteToRateWhenDrivingLessonCompletedHandler(CreateReader(), _fixture.CreateNotifier())
            .Handle(Raised<DrivingLessonCompletedDomainEvent>(), TestContext.Current.CancellationToken);
}
