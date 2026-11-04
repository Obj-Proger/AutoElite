namespace AutoElite.Application.Chats.RenameChat;

/// <summary>Renames the chat; the domain checks that the caller is its administrator.</summary>
internal sealed class RenameChatCommandHandler(ChatAccess chatAccess, ICurrentUser currentUser) : ICommandHandler<RenameChatCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(RenameChatCommand command, CancellationToken cancellationToken)
    {
        var chatResult = await chatAccess.LoadAsMemberAsync(command.ChatId, cancellationToken);

        return chatResult.IsFailure ? chatResult : chatResult.Value.Rename(currentUser.GetRequiredUserId(), command.Title);
    }
}
