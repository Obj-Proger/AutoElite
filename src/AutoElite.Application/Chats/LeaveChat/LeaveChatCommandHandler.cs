namespace AutoElite.Application.Chats.LeaveChat;

/// <summary>Leaves the chat; a personal chat cannot be left.</summary>
internal sealed class LeaveChatCommandHandler(ChatAccess chatAccess, ICurrentUser currentUser, TimeProvider timeProvider)
    : ICommandHandler<LeaveChatCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(LeaveChatCommand command, CancellationToken cancellationToken)
    {
        var chatResult = await chatAccess.LoadAsMemberAsync(command.ChatId, cancellationToken);

        return chatResult.IsFailure
            ? chatResult
            : chatResult.Value.Leave(currentUser.GetRequiredUserId(), timeProvider.GetUtcNow().UtcDateTime);
    }
}
