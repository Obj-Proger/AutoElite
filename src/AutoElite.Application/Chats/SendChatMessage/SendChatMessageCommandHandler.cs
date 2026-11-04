namespace AutoElite.Application.Chats.SendChatMessage;

/// <summary>Sends the message to the caller's chat.</summary>
internal sealed class SendChatMessageCommandHandler(
    ChatAccess chatAccess,
    IRepository<ChatMessage> messageRepository,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<SendChatMessageCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(SendChatMessageCommand command, CancellationToken cancellationToken)
    {
        var chatResult = await chatAccess.LoadAsMemberAsync(command.ChatId, cancellationToken);
        if (chatResult.IsFailure)
            return Result.Failure<Guid>(chatResult.Error);

        var messageResult = ChatMessage.Send(
            chatResult.Value, currentUser.GetRequiredUserId(), command.Text, [], timeProvider.GetUtcNow().UtcDateTime);
        if (messageResult.IsFailure)
            return Result.Failure<Guid>(messageResult.Error);

        messageRepository.Add(messageResult.Value);

        return messageResult.Value.Id;
    }
}
