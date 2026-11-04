namespace AutoElite.Application.Chats.CreateGroupChat;

/// <summary>Checks the members and creates the chat.</summary>
internal sealed class CreateGroupChatCommandHandler(
    IChatRepository chatRepository,
    ChatUsers chatUsers,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<CreateGroupChatCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateGroupChatCommand command, CancellationToken cancellationToken)
    {
        var memberIds = command.MemberIds ?? [];
        var chatResult = Chat.CreateGroup(
            command.Title, currentUser.GetRequiredUserId(), memberIds, null, timeProvider.GetUtcNow().UtcDateTime);
        if (chatResult.IsFailure)
            return Result.Failure<Guid>(chatResult.Error);

        var usersResult = await chatUsers.CheckActiveAsync(memberIds, cancellationToken);
        if (usersResult.IsFailure)
            return Result.Failure<Guid>(usersResult.Error);

        chatRepository.Add(chatResult.Value);

        return chatResult.Value.Id;
    }
}
