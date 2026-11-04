namespace AutoElite.Application.Chats.AddChatMember;

/// <summary>Checks the user and adds them; the domain checks that the caller is an administrator of the chat.</summary>
internal sealed class AddChatMemberCommandHandler(
    ChatAccess chatAccess,
    ChatUsers chatUsers,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<AddChatMemberCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(AddChatMemberCommand command, CancellationToken cancellationToken)
    {
        var chatResult = await chatAccess.LoadAsMemberAsync(command.ChatId, cancellationToken);
        if (chatResult.IsFailure)
            return chatResult;

        var addResult = chatResult.Value.AddMember(currentUser.GetRequiredUserId(), command.UserId, timeProvider.GetUtcNow().UtcDateTime);
        if (addResult.IsFailure)
            return addResult;

        return await chatUsers.CheckActiveAsync([command.UserId], cancellationToken);
    }
}
