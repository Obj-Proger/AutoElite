using AutoElite.Application.Notifications.Push;
using AutoElite.Application.Notifications.RegisterPushDevice;
using AutoElite.Application.Notifications.UnregisterPushDevice;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;

namespace AutoElite.Application.Tests.Notifications;

public class PushDeviceTests
{
    private static readonly string[] _bothTokens = ["token-1", "token-2"];

    private readonly FakePushDeviceRepository _devices = new();
    private readonly FakeNotificationRepository _notifications = new();
    private readonly FakePushSender _sender = new();
    private readonly FakeCurrentUser _user = new() { UserId = Guid.NewGuid() };

    private Task<Result> Register(string token, FakeCurrentUser? user = null) =>
        new RegisterPushDeviceCommandHandler(_devices, user ?? _user, TestData.Clock())
            .Handle(new RegisterPushDeviceCommand(token, PushPlatform.Android), TestContext.Current.CancellationToken);

    private Task<Result> Unregister(string token, FakeCurrentUser? user = null) =>
        new UnregisterPushDeviceCommandHandler(_devices, user ?? _user)
            .Handle(new UnregisterPushDeviceCommand(token), TestContext.Current.CancellationToken);

    private Notification Notify(Guid recipientId)
    {
        var subject = EntityReference.Create(EntityKind.Chat, Guid.NewGuid()).Value;
        var notification = Notification.Create(
            recipientId, NotificationType.NewMessage, "New message", "Hello", subject, TestData.Now).Value;
        _notifications.Seed(notification);

        return notification;
    }

    private Task Push(Notification notification, ListLogger<SendPushWhenNotificationCreatedHandler>? logger = null) =>
        new SendPushWhenNotificationCreatedHandler(
                _notifications, _devices, _sender, logger ?? new ListLogger<SendPushWhenNotificationCreatedHandler>())
            .Handle(
                new NotificationCreatedDomainEvent(notification.Id, notification.RecipientId, notification.Type, TestData.Now),
                TestContext.Current.CancellationToken);

    [Fact]
    public async Task Register_ANewToken_AddsTheDeviceOfTheUser()
    {
        var result = await Register(" token-1 ");

        result.IsSuccess.Should().BeTrue();
        var device = _devices.Items.Should().ContainSingle().Subject;
        device.Token.Should().Be("token-1");
        device.UserId.Should().Be(_user.UserId!.Value);
    }

    [Fact]
    public async Task Register_ATokenOfAnotherUser_PassesTheDevice()
    {
        var other = new FakeCurrentUser { UserId = Guid.NewGuid() };
        await Register("token-1", other);

        await Register("token-1");

        _devices.Items.Should().ContainSingle().Which.UserId.Should().Be(_user.UserId!.Value);
    }

    [Fact]
    public async Task Register_NoToken_ReturnsTokenEmptyError()
    {
        var result = await Register("  ");

        result.Error.Should().Be(DomainErrors.PushDevice.TokenEmpty);
        _devices.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Unregister_RemovesTheDeviceOfTheUserOnly()
    {
        var stranger = new FakeCurrentUser { UserId = Guid.NewGuid() };
        await Register("token-1");

        (await Unregister("token-1", stranger)).IsSuccess.Should().BeTrue();
        _devices.Items.Should().ContainSingle();
        (await Unregister("token-1")).IsSuccess.Should().BeTrue();
        _devices.Items.Should().BeEmpty();
        (await Unregister("unknown")).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Push_SendsTheNotificationToTheDevicesOfItsRecipient()
    {
        await Register("token-1");
        await Register("token-2");
        var notification = Notify(_user.UserId!.Value);

        await Push(notification);

        var (tokens, message) = _sender.Sent.Should().ContainSingle().Subject;
        tokens.Should().BeEquivalentTo(_bothTokens);
        message.Title.Should().Be("New message");
        message.Body.Should().Be("Hello");
        message.Data["notificationId"].Should().Be(notification.Id.ToString());
        message.Data["type"].Should().Be(nameof(NotificationType.NewMessage));
        message.Data["subjectKind"].Should().Be(nameof(EntityKind.Chat));
        message.Data["subjectId"].Should().Be(notification.Subject!.Id.ToString());
    }

    [Fact]
    public async Task Push_ARecipientWithoutDevices_SendsNothing()
    {
        await Push(Notify(Guid.NewGuid()));

        _sender.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Push_RemovesTheDevicesTheServiceNoLongerKnows()
    {
        await Register("token-1");
        await Register("token-2");
        _sender.UnknownTokens.Add("token-1");

        await Push(Notify(_user.UserId!.Value));

        _devices.Items.Should().ContainSingle().Which.Token.Should().Be("token-2");
    }

    [Fact]
    public async Task Push_AFailureOfTheService_IsLoggedAndKeepsTheDevices()
    {
        await Register("token-1");
        _sender.Failure = new HttpRequestException("unavailable");
        var logger = new ListLogger<SendPushWhenNotificationCreatedHandler>();

        await Push(Notify(_user.UserId!.Value), logger);

        logger.Entries.Should().ContainSingle(entry => entry.Level == LogLevel.Warning);
        _devices.Items.Should().ContainSingle();
    }
}
