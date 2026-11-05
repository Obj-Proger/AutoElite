using AutoElite.Application.Notifications.Chats;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;

namespace AutoElite.Application.Tests.Notifications;

public class ChatNotificationTests
{
    private readonly NotificationFixture _fixture = new();
    private readonly FakeChatRepository _chats = new();
    private readonly InMemoryRepository<ChatMessage> _messages = new();
    private readonly User _sender;
    private readonly User _recipient;

    public ChatNotificationTests()
    {
        _sender = _fixture.SeedUser("sender@example.com", firstName: "Pavel");
        _recipient = _fixture.SeedUser("recipient@example.com");
    }

    private Chat SeedDirectChat()
    {
        var chat = Chat.CreateDirect(_sender.Id, _recipient.Id, TestData.Now).Value;
        _chats.Seed(chat);

        return chat;
    }

    private ChatMessage Send(Chat chat, DateTime? sentAtUtc = null)
    {
        var message = ChatMessage.Send(chat, _sender.Id, "Hello", [], sentAtUtc ?? TestData.Now).Value;
        _messages.Seed(message);

        return message;
    }

    private Task Handle(ChatMessage message) =>
        new NotifyWhenChatMessageSentHandler(_chats, _messages, _fixture.Users, _fixture.Notifications, _fixture.CreateNotifier())
            .Handle(message.DomainEvents.OfType<ChatMessageSentDomainEvent>().Single(), TestContext.Current.CancellationToken);

    [Fact]
    public async Task DirectMessage_NotifiesTheRecipientButNotTheSender()
    {
        var chat = SeedDirectChat();

        await Handle(Send(chat));

        var notification = _fixture.Notifications.Items.Should().ContainSingle().Which;
        notification.RecipientId.Should().Be(_recipient.Id);
        notification.Type.Should().Be(NotificationType.NewMessage);
        notification.Title.Should().Be("Новые сообщения");
        notification.Body.Should().Be("Новые сообщения от Ivanov Pavel.");
        notification.Subject.Should().Be(EntityReference.Create(EntityKind.Chat, chat.Id).Value);
    }

    [Fact]
    public async Task GroupMessage_NamesTheChatAndSkipsMutedMembers()
    {
        var muted = _fixture.SeedUser("muted@example.com");
        var chat = Chat.CreateGroup("B-12", _sender.Id, [_recipient.Id, muted.Id], null, TestData.Now).Value;
        chat.SetMuted(muted.Id, true).IsSuccess.Should().BeTrue();
        _chats.Seed(chat);

        await Handle(Send(chat));

        _fixture.Notifications.Items.Should().ContainSingle()
            .Which.Should().Match<Notification>(notification => notification.RecipientId == _recipient.Id
                && notification.Body == "Новые сообщения в чате «B-12».");
    }

    [Fact]
    public async Task NextMessage_WhileTheNotificationIsUnread_AddsNothing()
    {
        var chat = SeedDirectChat();
        await Handle(Send(chat));

        await Handle(Send(chat, TestData.Now.AddMinutes(1)));

        _fixture.Notifications.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task NextMessage_AfterTheNotificationWasRead_NotifiesAgain()
    {
        var chat = SeedDirectChat();
        await Handle(Send(chat));
        _fixture.Notifications.Items.Single().MarkAsRead(TestData.Now.AddMinutes(1));

        await Handle(Send(chat, TestData.Now.AddMinutes(2)));

        _fixture.Of(_recipient.Id).Should().HaveCount(2);
    }

    [Fact]
    public async Task Message_AlreadyReadWhenTheEventIsHandled_NotifiesNobody()
    {
        var chat = SeedDirectChat();
        var message = Send(chat);
        chat.MarkRead(_recipient.Id, TestData.Now.AddMinutes(1)).IsSuccess.Should().BeTrue();

        await Handle(message);

        _fixture.Notifications.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Message_DeletedBeforeTheEventIsHandled_NotifiesNobody()
    {
        var message = Send(SeedDirectChat());
        message.Delete(_sender.Id, TestData.Now.AddMinutes(1)).IsSuccess.Should().BeTrue();

        await Handle(message);

        _fixture.Notifications.Items.Should().BeEmpty();
    }
}
