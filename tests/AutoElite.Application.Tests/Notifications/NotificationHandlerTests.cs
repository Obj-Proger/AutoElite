using System.Globalization;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Notifications;
using AutoElite.Application.Notifications.GetMyNotifications;
using AutoElite.Application.Notifications.GetUnreadNotificationCount;
using AutoElite.Application.Notifications.MarkAllNotificationsRead;
using AutoElite.Application.Notifications.MarkNotificationRead;
using AutoElite.Application.Notifications.Payments;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;

namespace AutoElite.Application.Tests.Notifications;

public class NotificationHandlerTests
{
    private readonly FakeNotificationRepository _notifications = new();
    private readonly FakeNotificationQueries _queries = new();
    private readonly FakeUserRepository _users = new();
    private readonly FakeContractRepository _contracts = new();
    private readonly User _student = TestData.NewUser("student@example.com");
    private readonly Contract _contract;

    public NotificationHandlerTests()
    {
        _users.Seed(_student);
        var course = TestData.NewCourse();
        _contract = Contract.Create(
                "AE-7", _student, course, course.Price, Transmission.Manual, null, new DateOnly(2026, 2, 1), new DateOnly(2026, 6, 30),
                TestData.Now)
            .Value;
        _contracts.Seed(_contract);
    }

    private FakeCurrentUser Student() => new() { UserId = _student.Id, Roles = new HashSet<UserRole> { UserRole.Student } };

    private Notifier CreateNotifier() => new(_users, _notifications);

    private Task Confirmed(Guid paymentId) =>
        new NotifyWhenPaymentConfirmedHandler(CreateNotifier(), _contracts).Handle(
            new PaymentConfirmedDomainEvent(paymentId, _contract.Id, _student.Id, 15000m, "RUB", TestData.Now),
            TestContext.Current.CancellationToken);

    private Notification SeedNotification(Guid recipientId)
    {
        var notification = Notification.Create(recipientId, NotificationType.Other, "Title", null, null, TestData.Now).Value;
        _notifications.Seed(notification);

        return notification;
    }

    [Fact]
    public async Task PaymentConfirmed_NotifiesTheStudentInTheirLanguage()
    {
        var paymentId = Guid.CreateVersion7();

        await Confirmed(paymentId);

        var notification = _notifications.Items.Should().ContainSingle().Which;
        notification.RecipientId.Should().Be(_student.Id);
        notification.Type.Should().Be(NotificationType.PaymentConfirmed);
        notification.Title.Should().Be("Платёж получен");
        notification.Body.Should().Contain($"{15000m.ToString("N2", CultureInfo.GetCultureInfo("ru"))} RUB").And.Contain("AE-7");
        notification.Subject.Should().Be(EntityReference.Create(EntityKind.Payment, paymentId).Value);
        notification.CreatedAtUtc.Should().Be(TestData.Now);
    }

    [Fact]
    public async Task PaymentRefunded_EnglishSpeakingStudent_NotifiesInEnglish()
    {
        var student = TestData.NewUser("english@example.com", language: Language.English);
        _users.Seed(student);

        await new NotifyWhenPaymentRefundedHandler(CreateNotifier(), _contracts).Handle(
            new PaymentRefundedDomainEvent(Guid.CreateVersion7(), _contract.Id, student.Id, 500m, "RUB", TestData.Now),
            TestContext.Current.CancellationToken);

        var notification = _notifications.Items.Should().ContainSingle().Which;
        notification.Title.Should().Be("Payment refunded");
        notification.Body.Should().Contain("500.00 RUB");
    }

    [Fact]
    public async Task PaymentConfirmed_DeactivatedStudent_CreatesNoNotification()
    {
        _student.Deactivate(TestData.Now);

        await Confirmed(Guid.CreateVersion7());

        _notifications.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task MarkRead_OwnNotification_MarksItAndKeepsTheFirstTime()
    {
        var notification = SeedNotification(_student.Id);
        notification.MarkAsRead(TestData.Now);

        var later = new FixedTimeProvider(new DateTimeOffset(TestData.Now.AddHours(1)));

        var result = await new MarkNotificationReadCommandHandler(_notifications, Student(), later)
            .Handle(new MarkNotificationReadCommand(notification.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        notification.ReadAtUtc.Should().Be(TestData.Now);
    }

    [Fact]
    public async Task MarkRead_NotificationOfAnotherUser_ReturnsNotificationNotFound()
    {
        var notification = SeedNotification(Guid.CreateVersion7());

        var result = await new MarkNotificationReadCommandHandler(_notifications, Student(), TestData.Clock())
            .Handle(new MarkNotificationReadCommand(notification.Id), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.NotFound<Notification>());
        notification.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task MarkAllRead_MarksOnlyTheCallersUnreadNotifications()
    {
        SeedNotification(_student.Id);
        SeedNotification(_student.Id).MarkAsRead(TestData.Now);
        var foreign = SeedNotification(Guid.CreateVersion7());

        var result = await new MarkAllNotificationsReadCommandHandler(_notifications, Student(), TestData.Clock())
            .Handle(new MarkAllNotificationsReadCommand(), TestContext.Current.CancellationToken);

        result.Value.Should().Be(1);
        foreign.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task GetMyNotifications_PassesTheCallerTheFilterAndThePage()
    {
        await new GetMyNotificationsQueryHandler(_queries, Student())
            .Handle(new GetMyNotificationsQuery(false, 2, 30), TestContext.Current.CancellationToken);

        _queries.LastPageCall.Should().Be((_student.Id, (bool?)false, new PageRequest(2, 30)));
    }

    [Fact]
    public async Task GetUnreadCount_ReturnsTheCount()
    {
        _queries.UnreadCount = 4;

        var result = await new GetUnreadNotificationCountQueryHandler(_queries, Student())
            .Handle(new GetUnreadNotificationCountQuery(), TestContext.Current.CancellationToken);

        result.Value.Should().Be(4);
    }
}
