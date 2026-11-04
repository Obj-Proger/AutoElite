namespace AutoElite.Application.Chats.MarkChatRead;

/// <summary>Moves the caller's read position, never beyond now.</summary>
internal sealed class MarkChatReadCommandHandler(ChatAccess chatAccess, ICurrentUser currentUser, TimeProvider timeProvider)
    : ICommandHandler<MarkChatReadCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(MarkChatReadCommand command, CancellationToken cancellationToken)
    {
        var chatResult = await chatAccess.LoadAsMemberAsync(command.ChatId, cancellationToken);
        if (chatResult.IsFailure)
            return chatResult;

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var readUpTo = command.ReadUpToUtc > utcNow ? utcNow : command.ReadUpToUtc;

        return chatResult.Value.MarkRead(currentUser.GetRequiredUserId(), readUpTo);
    }
}
