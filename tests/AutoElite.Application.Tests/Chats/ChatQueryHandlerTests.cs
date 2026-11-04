using AutoElite.Application.Chats;
using AutoElite.Application.Chats.GetChatMembers;
using AutoElite.Application.Chats.GetChatMessages;
using AutoElite.Application.Chats.GetMyChats;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Chats;

public class ChatQueryHandlerTests
{
    private readonly FakeChatQueries _queries = new();
    private readonly FakeCurrentUser _caller = FakeCurrentUser.SignedIn(UserRole.Student);
    private readonly Guid _chatId = Guid.CreateVersion7();

    private Task<Result<IReadOnlyList<ChatMessageItem>>> GetMessages(Guid? beforeMessageId = null) =>
        new GetChatMessagesQueryHandler(_queries, _caller)
            .Handle(new GetChatMessagesQuery(_chatId, beforeMessageId, 20), TestContext.Current.CancellationToken);

    [Fact]
    public async Task GetMyChats_SignedInUser_ReadsTheirChats()
    {
        var chat = new ChatListItem(
            _chatId, ChatKind.Direct, "Petrov Petr", Guid.CreateVersion7(), null, "See you", null, TestData.Now, 2, false,
            ChatMemberRole.Member);
        _queries.Chats.Add(chat);

        var result = await new GetMyChatsQueryHandler(_queries, _caller)
            .Handle(new GetMyChatsQuery(), TestContext.Current.CancellationToken);

        result.Value.Should().Equal(chat);
        _queries.LastUserId.Should().Be(_caller.UserId);
    }

    [Fact]
    public async Task GetMessages_Member_PassesTheCursorAndTheLimit()
    {
        _queries.Members.Add((_chatId, _caller.UserId!.Value));
        var cursor = Guid.CreateVersion7();

        var result = await GetMessages(cursor);

        result.IsSuccess.Should().BeTrue();
        _queries.LastMessagesCall.Should().Be((_chatId, (Guid?)cursor, 20));
    }

    [Fact]
    public async Task GetMessages_NotAMember_ReturnsChatNotFoundWithoutReading()
    {
        var result = await GetMessages();

        result.Error.Should().Be(ApplicationErrors.NotFound<Chat>());
        _queries.LastMessagesCall.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void MessagesValidator_LimitOutOfRange_ReportsPageSizeOutOfRange(int limit)
    {
        var result = new GetChatMessagesQueryValidator().Validate(new GetChatMessagesQuery(_chatId, Limit: limit));

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(ApplicationErrors.Paging.PageSizeOutOfRange.Code);
    }

    [Fact]
    public async Task GetMembers_NotAMember_ReturnsChatNotFound()
    {
        var result = await new GetChatMembersQueryHandler(_queries, _caller)
            .Handle(new GetChatMembersQuery(_chatId), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.NotFound<Chat>());
    }
}
