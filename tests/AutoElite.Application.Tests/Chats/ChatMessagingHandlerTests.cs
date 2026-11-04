using AutoElite.Application.Chats;
using AutoElite.Application.Chats.DeleteChatMessage;
using AutoElite.Application.Chats.MarkChatRead;
using AutoElite.Application.Chats.MuteChat;
using AutoElite.Application.Chats.SendChatMessage;
using AutoElite.Application.Chats.SendDirectMessage;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;

namespace AutoElite.Application.Tests.Chats;

public class ChatMessagingHandlerTests
{
    private readonly FakeChatRepository _chats = new();
    private readonly InMemoryRepository<ChatMessage> _messages = new();
    private readonly FakeUserRepository _users = new();
    private readonly User _sender = TestData.NewUser("sender@example.com");
    private readonly User _recipient = TestData.NewUser("recipient@example.com");

    public ChatMessagingHandlerTests()
    {
        _users.Seed(_sender);
        _users.Seed(_recipient);
    }

    private static FakeCurrentUser SignedIn(User user) => new() { UserId = user.Id, Roles = new HashSet<UserRole> { UserRole.Student } };

    private ChatAccess Access(FakeCurrentUser caller) => new(_chats, caller);

    private Task<Result<Guid>> SendDirect(string text, User? recipient = null, User? sender = null) =>
        new SendDirectMessageCommandHandler(_chats, _messages, _users, SignedIn(sender ?? _sender), TestData.Clock())
            .Handle(new SendDirectMessageCommand((recipient ?? _recipient).Id, text), TestContext.Current.CancellationToken);

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

        var result = await new SendChatMessageCommandHandler(Access(caller), _messages, caller, TestData.Clock())
            .Handle(new SendChatMessageCommand(chat.Id, "On my way"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _messages.Items.Should().ContainSingle().Which.SenderId.Should().Be(_recipient.Id);
    }

    [Fact]
    public async Task SendToChat_NotAMember_ReturnsChatNotFound()
    {
        var chat = SeedGroupChat();
        var stranger = SignedIn(TestData.NewUser("stranger@example.com"));

        var result = await new SendChatMessageCommandHandler(Access(stranger), _messages, stranger, TestData.Clock())
            .Handle(new SendChatMessageCommand(chat.Id, "Hi"), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.NotFound<Chat>());
    }

    [Fact]
    public async Task Delete_OwnMessage_ErasesItForEveryone()
    {
        var messageId = (await SendDirect("Oops")).Value;
        var message = _messages.Items.Single();
        var caller = SignedIn(_sender);

        var result = await new DeleteChatMessageCommandHandler(_messages, Access(caller), caller, TestData.Clock())
            .Handle(new DeleteChatMessageCommand(messageId), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        message.IsDeleted.Should().BeTrue();
        message.Text.Should().BeNull();
    }

    [Fact]
    public async Task Delete_MessageOfTheOtherMember_ReturnsNotSender()
    {
        var messageId = (await SendDirect("Mine")).Value;
        var caller = SignedIn(_recipient);

        var result = await new DeleteChatMessageCommandHandler(_messages, Access(caller), caller, TestData.Clock())
            .Handle(new DeleteChatMessageCommand(messageId), TestContext.Current.CancellationToken);

        result.Error.Should().Be(DomainErrors.ChatMessage.NotSender);
    }

    [Fact]
    public async Task Delete_MessageOfAnotherChat_ReturnsChatMessageNotFound()
    {
        var messageId = (await SendDirect("Private")).Value;
        var stranger = SignedIn(TestData.NewUser("stranger@example.com"));

        var result = await new DeleteChatMessageCommandHandler(_messages, Access(stranger), stranger, TestData.Clock())
            .Handle(new DeleteChatMessageCommand(messageId), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.NotFound<ChatMessage>());
    }

    [Fact]
    public async Task MarkRead_FutureTime_CountsAsNow()
    {
        var chat = SeedGroupChat(_recipient);
        var caller = SignedIn(_recipient);

        var result = await new MarkChatReadCommandHandler(Access(caller), caller, TestData.Clock())
            .Handle(new MarkChatReadCommand(chat.Id, TestData.Now.AddDays(1)), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        chat.CurrentMembers.Single(member => member.UserId == _recipient.Id).LastReadAtUtc.Should().Be(TestData.Now);
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
