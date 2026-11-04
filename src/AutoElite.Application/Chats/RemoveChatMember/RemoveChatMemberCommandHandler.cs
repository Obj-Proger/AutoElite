namespace AutoElite.Application.Chats.RemoveChatMember;

/// <summary>Removes the member; the domain checks that the caller is an administrator of the chat.</summary>
internal sealed class RemoveChatMemberCommandHandler(ChatAccess chatAccess, ICurrentUser currentUser, TimeProvider timeProvider)
    : ICommandHandler<RemoveChatMemberCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(RemoveChatMemberCommand command, CancellationToken cancellationToken)
    {
        var chatResult = await chatAccess.LoadAsMemberAsync(command.ChatId, cancellationToken);

        return chatResult.IsFailure
            ? chatResult
            : chatResult.Value.RemoveMember(currentUser.GetRequiredUserId(), command.UserId, timeProvider.GetUtcNow().UtcDateTime);
    }
}
