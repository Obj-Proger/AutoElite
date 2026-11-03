namespace AutoElite.Domain.Tests.Entities;

public class NotificationTests
{
    private static readonly DateTime _now = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _later = _now.AddHours(2);
    private static readonly Guid _recipientId = new("11111111-1111-1111-1111-111111111111");
    private static readonly EntityReference _exam = EntityReference.Create(EntityKind.Exam, new("55555555-5555-5555-5555-555555555555")).Value;

    private static Notification CreateNotification() =>
        Notification.Create(_recipientId, NotificationType.ExamChanged, "Exam scheduled", null, _exam, _now).Value;

    [Fact]
    public void Create_ValidData_ReturnsUnreadNotificationAndRaisesCreatedEvent()
    {
        var result = Notification.Create(_recipientId, NotificationType.ExamChanged, "  Exam scheduled  ", "  20 April, 10:00  ", _exam, _now);

        result.IsSuccess.Should().BeTrue();
        var notification = result.Value;
        notification.RecipientId.Should().Be(_recipientId);
        notification.Type.Should().Be(NotificationType.ExamChanged);
        notification.Title.Should().Be("Exam scheduled");
        notification.Body.Should().Be("20 April, 10:00");
        notification.Subject.Should().Be(_exam);
        notification.CreatedAtUtc.Should().Be(_now);
        notification.IsRead.Should().BeFalse();
        notification.DomainEvents.Should().ContainSingle().Which.Should()
            .Be(new NotificationCreatedDomainEvent(notification.Id, _recipientId, NotificationType.ExamChanged, _now));
    }

    [Fact]
    public void Create_EmptyRecipient_ReturnsRecipientIdEmptyError()
    {
        var result = Notification.Create(Guid.Empty, NotificationType.Other, "Title", null, null, _now);

        result.Error.Should().Be(DomainErrors.Notification.RecipientIdEmpty);
    }

    [Fact]
    public void Create_UndefinedType_ReturnsTypeNotDefinedError()
    {
        var result = Notification.Create(_recipientId, (NotificationType)0, "Title", null, null, _now);

        result.Error.Should().Be(DomainErrors.Notification.TypeNotDefined);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_EmptyTitle_ReturnsTitleEmptyError(string? title)
    {
        var result = Notification.Create(_recipientId, NotificationType.Other, title, null, null, _now);

        result.Error.Should().Be(DomainErrors.Notification.TitleEmpty);
    }

    [Fact]
    public void Create_BodyTooLong_ReturnsBodyTooLongError()
    {
        var result = Notification.Create(_recipientId, NotificationType.Other, "Title", new string('a', 2001), null, _now);

        result.Error.Should().Be(DomainErrors.Notification.BodyTooLong);
    }

    [Fact]
    public void MarkAsRead_Unread_RecordsReadTime()
    {
        var notification = CreateNotification();

        notification.MarkAsRead(_later);

        notification.IsRead.Should().BeTrue();
        notification.ReadAtUtc.Should().Be(_later);
    }

    [Fact]
    public void MarkAsRead_AlreadyRead_KeepsFirstReadTime()
    {
        var notification = CreateNotification();
        notification.MarkAsRead(_now);

        notification.MarkAsRead(_later);

        notification.ReadAtUtc.Should().Be(_now);
    }
}
