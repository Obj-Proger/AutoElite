using AutoElite.Application.Chats;
using AutoElite.Application.Chats.DeleteChatMessage;
using AutoElite.Application.Chats.MarkChatRead;
using AutoElite.Application.Chats.MuteChat;
using AutoElite.Application.Chats.OpenDirectChat;
using AutoElite.Application.Chats.SendChatMessage;
using AutoElite.Application.Chats.SendDirectMessage;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;

namespace AutoElite.Application.Tests.Chats;

public class ChatMessagingHandlerTests
{
    private readonly InMemoryRepository<ChatMessage> _messages = new();
    private readonly FakeChatRepository _chats;
    private readonly FakeDocumentRepository _documents = new();
    private readonly FakeUserRepository _users = new();
    private readonly FakeNotificationRepository _notifications = new();
    private readonly User _sender = TestData.NewUser("sender@example.com");
    private readonly User _recipient = TestData.NewUser("recipient@example.com");

    public ChatMessagingHandlerTests()
    {
        _chats = new FakeChatRepository { Messages = _messages };
        _users.Seed(_sender);
        _users.Seed(_recipient);
    }

    private static FakeCurrentUser SignedIn(User user) => new() { UserId = user.Id, Roles = new HashSet<UserRole> { UserRole.Student } };

    private ChatAccess Access(FakeCurrentUser caller) => new(_chats, caller);

    private ChatAttachments Attachments() => new(_documents, _chats);

    private Task<Result<Guid>> SendDirect(string text, User? recipient = null, User? sender = null)
    {
        var caller = SignedIn(sender ?? _sender);

        return new SendDirectMessageCommandHandler(new DirectChats(_chats, _users, caller), _messages, caller, TestData.Clock())
            .Handle(new SendDirectMessageCommand((recipient ?? _recipient).Id, text), TestContext.Current.CancellationToken);
    }

    private Task<Result<Guid>> SendToChat(Chat chat, FakeCurrentUser caller, string? text, params Guid[] attachmentIds) =>
        new SendChatMessageCommandHandler(Access(caller), Attachments(), _messages, caller, TestData.Clock())
            .Handle(new SendChatMessageCommand(chat.Id, text, attachmentIds), TestContext.Current.CancellationToken);

    private Task<Result> DeleteMessage(Guid messageId, FakeCurrentUser caller) =>
        new DeleteChatMessageCommandHandler(_messages, Access(caller), Attachments(), caller, TestData.Clock())
            .Handle(new DeleteChatMessageCommand(messageId), TestContext.Current.CancellationToken);

    private Document SeedAttachment(Chat chat, User uploadedBy)
    {
        var owner = EntityReference.Create(EntityKind.Chat, chat.Id).Value;
        var document = Document.Attach(
                owner, "route.png", "image/png", 10, $"documents/{Guid.CreateVersion7():N}", null, uploadedBy.Id, TestData.Now)
            .Value;
        _documents.Seed(document);

        return document;
    }

    private Chat SeedGroupChat(params User[] members)
    {
        var chat = Chat.CreateGroup("B-2026-03", _sender.Id, [.. members.Select(member => member.Id)], null, TestData.Now).Value;
        _chats.Seed(chat);

        return chat;
    }

    [Fact]
    public async Task SendDirect_NewRecipient_CreatesThePersonalChatAndSendsTheMessage()
    {
        var result = await SendDirect("Hello!");

        var chat = _chats.Items.Should().ContainSingle().Which;
        chat.Kind.Should().Be(ChatKind.Direct);
        chat.DirectKey.Should().Be(Chat.BuildDirectKey(_sender.Id, _recipient.Id));
        var message = _messages.Items.Should().ContainSingle().Which;
        message.Id.Should().Be(result.Value);
        message.ChatId.Should().Be(chat.Id);
        message.Text.Should().Be("Hello!");
        message.DomainEvents.Should().ContainSingle(domainEvent => domainEvent is ChatMessageSentDomainEvent);
    }

    [Fact]
    public async Task SendDirect_ReplyOfTheRecipient_ReusesTheChatOfThePair()
    {
        await SendDirect("Hello!");

        await SendDirect("Hi!", recipient: _sender, sender: _recipient);

        _chats.Items.Should().ContainSingle();
        _messages.Items.Select(message => message.ChatId).Distinct().Should().ContainSingle();
    }

    [Fact]
    public async Task SendDirect_DeactivatedRecipient_ReturnsUserNotFound()
    {
        _recipient.Deactivate(TestData.Now);

        var result = await SendDirect("Hello!");

        result.Error.Should().Be(ApplicationErrors.NotFound<User>());
        _chats.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task SendDirect_ToSelf_ReturnsDirectChatWithSelf()
    {
        var result = await SendDirect("Note to self", recipient: _sender);

        result.Error.Should().Be(DomainErrors.Chat.DirectChatWithSelf);
    }

    [Fact]
    public async Task SendDirect_WithoutText_ReturnsMessageEmpty()
    {
        var result = await SendDirect("   ");

        result.Error.Should().Be(DomainErrors.ChatMessage.Empty);
        _messages.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task SendToChat_Member_SendsTheMessage()
    {
        var chat = SeedGroupChat(_recipient);
        var caller = SignedIn(_recipient);

        var result = await SendToChat(chat, caller, "On my way");

        result.IsSuccess.Should().BeTrue();
        _messages.Items.Should().ContainSingle().Which.SenderId.Should().Be(_recipient.Id);
    }

    [Fact]
    public async Task SendToChat_NotAMember_ReturnsChatNotFound()
    {
        var chat = SeedGroupChat();
        var stranger = SignedIn(TestData.NewUser("stranger@example.com"));

        var result = await SendToChat(chat, stranger, "Hi");

        result.Error.Should().Be(ApplicationErrors.NotFound<Chat>());
    }

    [Fact]
    public async Task SendToChat_OwnDocumentsOfTheChat_AttachesThem()
    {
        var chat = SeedGroupChat(_recipient);
        var photo = SeedAttachment(chat, _recipient);

        var result = await SendToChat(chat, SignedIn(_recipient), null, photo.Id);

        result.IsSuccess.Should().BeTrue();
        _messages.Items.Single().AttachmentIds.Should().Equal(photo.Id);
    }

    [Fact]
    public async Task SendToChat_DocumentOfAnotherChat_ReturnsAttachmentInvalid()
    {
        var chat = SeedGroupChat(_recipient);
        var otherChat = SeedGroupChat(_recipient);
        var photo = SeedAttachment(otherChat, _recipient);

        var result = await SendToChat(chat, SignedIn(_recipient), null, photo.Id);

        result.Error.Should().Be(ApplicationErrors.Chat.AttachmentInvalid);
        _messages.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task SendToChat_DocumentAttachedBySomeoneElse_ReturnsAttachmentInvalid()
    {
        var chat = SeedGroupChat(_recipient);
        var photo = SeedAttachment(chat, _sender);

        var result = await SendToChat(chat, SignedIn(_recipient), null, photo.Id);

        result.Error.Should().Be(ApplicationErrors.Chat.AttachmentInvalid);
    }

    [Fact]
    public async Task SendToChat_UnknownDocument_ReturnsAttachmentInvalid()
    {
        var chat = SeedGroupChat(_recipient);

        var result = await SendToChat(chat, SignedIn(_recipient), "See", Guid.CreateVersion7());

        result.Error.Should().Be(ApplicationErrors.Chat.AttachmentInvalid);
    }

    [Fact]
    public async Task SendToChat_DocumentSentBefore_ReturnsAttachmentAlreadySent()
    {
        var chat = SeedGroupChat(_recipient);
        var caller = SignedIn(_recipient);
        var photo = SeedAttachment(chat, _recipient);
        (await SendToChat(chat, caller, null, photo.Id)).IsSuccess.Should().BeTrue();

        var result = await SendToChat(chat, caller, "Again", photo.Id);

        result.Error.Should().Be(ApplicationErrors.Chat.AttachmentAlreadySent);
    }

    [Fact]
    public async Task Delete_MessageWithAttachments_DeletesTheDocuments()
    {
        var chat = SeedGroupChat(_recipient);
        var caller = SignedIn(_recipient);
        var photo = SeedAttachment(chat, _recipient);
        var messageId = (await SendToChat(chat, caller, null, photo.Id)).Value;

        var result = await DeleteMessage(messageId, caller);

        result.IsSuccess.Should().BeTrue();
        _documents.Items.Should().BeEmpty();
        photo.DomainEvents.OfType<DocumentDeletedDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public async Task OpenDirectChat_CreatesTheChatOnceAndReturnsItAfterwards()
    {
        var caller = SignedIn(_sender);
        var handler = new OpenDirectChatCommandHandler(new DirectChats(_chats, _users, caller), TestData.Clock());

        var first = await handler.Handle(new OpenDirectChatCommand(_recipient.Id), TestContext.Current.CancellationToken);
        var second = await handler.Handle(new OpenDirectChatCommand(_recipient.Id), TestContext.Current.CancellationToken);

        first.Value.Should().Be(second.Value);
        _chats.Items.Should().ContainSingle().Which.Id.Should().Be(first.Value);
    }

    [Fact]
    public async Task Delete_OwnMessage_ErasesItForEveryone()
    {
        var messageId = (await SendDirect("Oops")).Value;
        var message = _messages.Items.Single();
        var caller = SignedIn(_sender);

        var result = await DeleteMessage(messageId, caller);

        result.IsSuccess.Should().BeTrue();
        message.IsDeleted.Should().BeTrue();
        message.Text.Should().BeNull();
    }

    [Fact]
    public async Task Delete_MessageOfTheOtherMember_ReturnsNotSender()
    {
        var messageId = (await SendDirect("Mine")).Value;
        var caller = SignedIn(_recipient);

        var result = await DeleteMessage(messageId, caller);

        result.Error.Should().Be(DomainErrors.ChatMessage.NotSender);
    }

    [Fact]
    public async Task Delete_MessageOfAnotherChat_ReturnsChatMessageNotFound()
    {
        var messageId = (await SendDirect("Private")).Value;
        var stranger = SignedIn(TestData.NewUser("stranger@example.com"));

        var result = await DeleteMessage(messageId, stranger);

        result.Error.Should().Be(ApplicationErrors.NotFound<ChatMessage>());
    }

    [Fact]
    public async Task MarkRead_FutureTime_CountsAsNow()
    {
        var chat = SeedGroupChat(_recipient);
        var caller = SignedIn(_recipient);

        var result = await MarkRead(chat, caller, TestData.Now.AddDays(1));

        result.IsSuccess.Should().BeTrue();
        chat.CurrentMembers.Single(member => member.UserId == _recipient.Id).LastReadAtUtc.Should().Be(TestData.Now);
    }

    [Fact]
    public async Task MarkRead_MarksTheMessageNotificationsOfTheChatUpToTheReadTime()
    {
        var chat = SeedGroupChat(_recipient);
        var caller = SignedIn(_recipient);
        var aboutChat = EntityReference.Create(EntityKind.Chat, chat.Id).Value;
        var read = SeedNotification(aboutChat, TestData.Now.AddMinutes(-10));
        var later = SeedNotification(aboutChat, TestData.Now.AddMinutes(-1));
        var aboutOtherChat = EntityReference.Create(EntityKind.Chat, Guid.CreateVersion7()).Value;
        var otherChat = SeedNotification(aboutOtherChat, TestData.Now.AddMinutes(-10));

        await MarkRead(chat, caller, TestData.Now.AddMinutes(-5));

        read.IsRead.Should().BeTrue();
        later.IsRead.Should().BeFalse();
        otherChat.IsRead.Should().BeFalse();
    }

    private Task<Result> MarkRead(Chat chat, FakeCurrentUser caller, DateTime readUpToUtc) =>
        new MarkChatReadCommandHandler(Access(caller), _notifications, caller, TestData.Clock())
            .Handle(new MarkChatReadCommand(chat.Id, readUpToUtc), TestContext.Current.CancellationToken);

    private Notification SeedNotification(EntityReference subject, DateTime createdAtUtc)
    {
        var notification = Notification.Create(_recipient.Id, NotificationType.NewMessage, "New", null, subject, createdAtUtc).Value;
        _notifications.Seed(notification);

        return notification;
    }

    [Fact]
    public void MarkReadValidator_LocalTime_ReportsNotUtc()
    {
        var command = new MarkChatReadCommand(Guid.CreateVersion7(), DateTime.SpecifyKind(TestData.Now, DateTimeKind.Local));

        var result = new MarkChatReadCommandValidator().Validate(command);

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(DomainErrors.TimeInterval.NotUtc.Code);
    }

    [Fact]
    public async Task Mute_Member_TurnsTheNotificationsOff()
    {
        var chat = SeedGroupChat(_recipient);
        var caller = SignedIn(_recipient);

        var result = await new MuteChatCommandHandler(Access(caller), caller)
            .Handle(new MuteChatCommand(chat.Id, true), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        chat.CurrentMembers.Single(member => member.UserId == _recipient.Id).IsMuted.Should().BeTrue();
    }
}
