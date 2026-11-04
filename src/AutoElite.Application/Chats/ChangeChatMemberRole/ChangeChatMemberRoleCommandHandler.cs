namespace AutoElite.Application.Chats.ChangeChatMemberRole;

/// <summary>Changes the role; the domain checks the caller, the member and the last administrator.</summary>
internal sealed class ChangeChatMemberRoleCommandHandler(ChatAccess chatAccess, ICurrentUser currentUser)
    : ICommandHandler<ChangeChatMemberRoleCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(ChangeChatMemberRoleCommand command, CancellationToken cancellationToken)
    {
        var chatResult = await chatAccess.LoadAsMemberAsync(command.ChatId, cancellationToken);

        return chatResult.IsFailure
            ? chatResult
            : chatResult.Value.ChangeRole(currentUser.GetRequiredUserId(), command.UserId, command.Role);
    }
}
