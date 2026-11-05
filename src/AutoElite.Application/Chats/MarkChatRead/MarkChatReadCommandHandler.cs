using AutoElite.Application.Notifications;

namespace AutoElite.Application.Chats.MarkChatRead;

/// <summary>
/// Moves the caller's read position, never beyond now, and marks as read the caller's new-message notifications
/// about the chat that the read messages caused.
/// </summary>
internal sealed class MarkChatReadCommandHandler(
    ChatAccess chatAccess,
    INotificationRepository notificationRepository,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<MarkChatReadCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(MarkChatReadCommand command, CancellationToken cancellationToken)
    {
        var chatResult = await chatAccess.LoadAsMemberAsync(command.ChatId, cancellationToken);
        if (chatResult.IsFailure)
            return chatResult;

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var readUpTo = command.ReadUpToUtc > utcNow ? utcNow : command.ReadUpToUtc;
        var chat = chatResult.Value;
        var userId = currentUser.GetRequiredUserId();

        var readResult = chat.MarkRead(userId, readUpTo);
        if (readResult.IsFailure)
            return readResult;

        await notificationRepository.MarkReadAboutAsync(
            userId, EntityReference.Create(EntityKind.Chat, chat.Id).Value, readUpTo, utcNow, cancellationToken);

        return Result.Success();
    }
}
