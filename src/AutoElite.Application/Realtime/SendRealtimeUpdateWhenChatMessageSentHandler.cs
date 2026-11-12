using AutoElite.Application.Chats;
using AutoElite.Application.Common.Events;

namespace AutoElite.Application.Realtime;

/// <summary>
/// Tells the connected clients of the current members of a chat about a new message, the sender's included, so that
/// their other devices show it too; a muted chat is updated as well, since muting only silences the notifications.
/// </summary>
/// <param name="chatRepository">The chats.</param>
/// <param name="realtimeUpdates">The channel to the connected clients.</param>
internal sealed class SendRealtimeUpdateWhenChatMessageSentHandler(IChatRepository chatRepository, IRealtimeUpdates realtimeUpdates)
    : IDomainEventHandler<ChatMessageSentDomainEvent>
{
    public async Task Handle(ChatMessageSentDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (await chatRepository.GetByIdAsync(domainEvent.ChatId, cancellationToken) is not { } chat)
            return;

        var memberIds = chat.CurrentMembers.Select(member => member.UserId).ToList();
        await realtimeUpdates.SendChatMessageAsync(
            memberIds, new ChatMessageUpdate(domainEvent.ChatId, domainEvent.MessageId, domainEvent.SenderId), cancellationToken);
    }
}
