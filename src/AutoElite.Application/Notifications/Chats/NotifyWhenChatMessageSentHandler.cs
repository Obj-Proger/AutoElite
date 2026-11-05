using AutoElite.Application.Chats;
using AutoElite.Application.Common.Events;
using AutoElite.Application.Common.Localization;
using AutoElite.Application.Users;

namespace AutoElite.Application.Notifications.Chats;

/// <summary>
/// Tells the members of a chat that new messages arrived (FR-10.1). The sender, members who muted the chat (FR-9.6)
/// and members who have already read the message are not notified.
/// </summary>
/// <remarks>
/// A member gets at most one unread notification per chat: while it stays unread, further messages add nothing, so a
/// busy group chat does not flood the list. Reading the chat marks the notification as read, and the next message
/// creates a new one.
/// </remarks>
internal sealed class NotifyWhenChatMessageSentHandler(
    IChatRepository chatRepository,
    IRepository<ChatMessage> messageRepository,
    IUserRepository userRepository,
    INotificationRepository notificationRepository,
    Notifier notifier) : IDomainEventHandler<ChatMessageSentDomainEvent>
{
    /// <inheritdoc />
    public async Task Handle(ChatMessageSentDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var chat = await chatRepository.GetByIdAsync(domainEvent.ChatId, cancellationToken);
        var message = await messageRepository.GetByIdAsync(domainEvent.MessageId, cancellationToken);
        if (chat is null || message is not { IsDeleted: false })
            return;

        var recipientIds = chat.CurrentMembers
            .Where(member => member.UserId != domainEvent.SenderId && !member.IsMuted
                && (member.LastReadAtUtc is null || member.LastReadAtUtc < message.SentAtUtc))
            .Select(member => member.UserId)
            .ToList();
        if (recipientIds.Count == 0)
            return;

        var subject = EntityReference.Create(EntityKind.Chat, chat.Id).Value;
        var alreadyNotified = await notificationRepository.GetRecipientsWithUnreadAsync(
            recipientIds, NotificationType.NewMessage, subject, cancellationToken);
        recipientIds.RemoveAll(alreadyNotified.Contains);
        if (recipientIds.Count == 0)
            return;

        if (chat.Kind == ChatKind.Direct)
        {
            var sender = await userRepository.GetByIdAsync(domainEvent.SenderId, cancellationToken);
            if (sender is null)
                return;

            await notifier.NotifyAllAsync(
                recipientIds, NotificationType.NewMessage, subject, NotificationTexts.NewDirectMessages,
                domainEvent.OccurredOnUtc, cancellationToken, sender.FullName.ToString());
        }
        else
        {
            await notifier.NotifyAllAsync(
                recipientIds, NotificationType.NewMessage, subject, NotificationTexts.NewGroupMessages,
                domainEvent.OccurredOnUtc, cancellationToken, chat.Title);
        }
    }
}
