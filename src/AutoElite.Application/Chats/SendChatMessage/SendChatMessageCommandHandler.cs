namespace AutoElite.Application.Chats.SendChatMessage;

/// <summary>Checks the attachments and sends the message to the caller's chat.</summary>
internal sealed class SendChatMessageCommandHandler(
    ChatAccess chatAccess,
    ChatAttachments chatAttachments,
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

        var chat = chatResult.Value;
        var senderId = currentUser.GetRequiredUserId();
        var attachmentIds = command.AttachmentIds ?? [];
        var messageResult = ChatMessage.Send(chat, senderId, command.Text, attachmentIds, timeProvider.GetUtcNow().UtcDateTime);
        if (messageResult.IsFailure)
            return Result.Failure<Guid>(messageResult.Error);

        var attachmentsResult = await chatAttachments.CheckAsync(chat, senderId, attachmentIds, cancellationToken);
        if (attachmentsResult.IsFailure)
            return Result.Failure<Guid>(attachmentsResult.Error);

        messageRepository.Add(messageResult.Value);

        return messageResult.Value.Id;
    }
}
