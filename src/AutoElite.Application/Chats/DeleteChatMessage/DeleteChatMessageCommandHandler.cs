namespace AutoElite.Application.Chats.DeleteChatMessage;

/// <summary>
/// Hides messages of chats the caller is not a member of as not found and deletes the message; the domain allows only
/// the sender to delete it. The attached documents are deleted with it, together with their files.
/// </summary>
internal sealed class DeleteChatMessageCommandHandler(
    IRepository<ChatMessage> messageRepository,
    ChatAccess chatAccess,
    ChatAttachments chatAttachments,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<DeleteChatMessageCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(DeleteChatMessageCommand command, CancellationToken cancellationToken)
    {
        var message = await messageRepository.GetByIdAsync(command.MessageId, cancellationToken);
        if (message is null || (await chatAccess.LoadAsMemberAsync(message.ChatId, cancellationToken)).IsFailure)
            return Result.Failure(ApplicationErrors.NotFound<ChatMessage>());

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        Guid[] attachmentIds = [.. message.AttachmentIds];
        var deleteResult = message.Delete(currentUser.GetRequiredUserId(), utcNow);
        if (deleteResult.IsFailure)
            return deleteResult;

        await chatAttachments.DeleteAsync(attachmentIds, utcNow, cancellationToken);

        return Result.Success();
    }
}
