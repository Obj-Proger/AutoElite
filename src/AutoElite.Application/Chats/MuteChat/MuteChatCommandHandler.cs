namespace AutoElite.Application.Chats.MuteChat;

/// <summary>Turns the caller's notifications of the chat off or on.</summary>
internal sealed class MuteChatCommandHandler(ChatAccess chatAccess, ICurrentUser currentUser) : ICommandHandler<MuteChatCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(MuteChatCommand command, CancellationToken cancellationToken)
    {
        var chatResult = await chatAccess.LoadAsMemberAsync(command.ChatId, cancellationToken);

        return chatResult.IsFailure ? chatResult : chatResult.Value.SetMuted(currentUser.GetRequiredUserId(), command.IsMuted);
    }
}
